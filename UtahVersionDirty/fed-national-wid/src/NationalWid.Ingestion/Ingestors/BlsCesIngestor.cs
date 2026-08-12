using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Npgsql;
using NationalWid.Ingestion.Models;
using NationalWid.Ingestion.Services;

namespace NationalWid.Ingestion.Ingestors;

/// <summary>
/// Fetches national + state CES series and upserts into the
/// <c>ces</c> table.
/// </summary>
public sealed class BlsCesIngestor(
    HttpClient httpClient,
    string? blsApiKey,
    string connectionString,
    SourceHashService sourceHashService,
    ILogger logger)
{
    // Top national CES series: total nonfarm + major supersectors (seasonally adjusted).
    private static readonly string[] Series =
    [
        "CES0000000001", // Total nonfarm employment
        "CES0500000001", // Total private
        "CES0600000001", // Goods-producing
        "CES0700000001", // Service-providing
        "CES1000000001", // Mining and logging
        "CES2000000001", // Construction
        "CES3000000001", // Manufacturing
        "CES4000000001", // Trade, transportation, and utilities
        "CES4100000001", // Retail trade
        "CES4200000001", // Wholesale trade
        "CES4300000001", // Transportation and warehousing
        "CES5000000001", // Information
        "CES5500000001", // Financial activities
        "CES6000000001", // Professional and business services
        "CES6500000001", // Education and health services
        "CES7000000001", // Leisure and hospitality
        "CES8000000001", // Other services
        "CES9000000001", // Government
        "CES9091000001", // Federal government
        "CES9092000001", // State government
    ];

    // 50 states + DC (2-digit FIPS).
    private static readonly string[] StateFipsCodes =
    [
        "01", "02", "04", "05", "06", "08", "09", "10", "11", "12", "13", "15", "16", "17", "18", "19",
        "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31", "32", "33", "34", "35",
        "36", "37", "38", "39", "40", "41", "42", "44", "45", "46", "47", "48", "49", "50", "51", "53",
        "54", "55", "56",
    ];

    private const string SeriesCodeType = "NAICS"; // WID CES series code type

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var currentYear = DateTime.UtcNow.Year;
        var startYear = currentYear - 20;
        var rowsByKey = new Dictionary<string, CesRow>(StringComparer.Ordinal);

        var flatFileService = new BlsFlatFileService(httpClient, logger, sourceHashService, connectionString);

        // National CE series (historical behavior).
        var flatSeries = await flatFileService.TryLoadSeriesAsync(
            dataset: "ce",
            dataFile: "ce.data.0.AllCESSeries",
            seriesIds: Series,
            startYear: startYear,
            endYear: currentYear,
            cancellationToken: cancellationToken);

        if (flatSeries.Count > 0)
        {
            MergeRows(rowsByKey, BuildNationalRows(flatSeries));
            logger.LogInformation("CES: loaded {Count} national rows from CE flat file", rowsByKey.Count);
        }

        // Statewide SM series using a dedicated state-total source that consistently
        // completes in the 15-minute ingestion window.
        logger.LogInformation("CES: starting state-level SM flat-file processing (sm.data.55)");
        await flatFileService.ProcessDataFileAsync(
            dataset: "sm",
            dataFile: "sm.data.55.TotalNonFarmStateWide.All",
            startYear: startYear,
            endYear: currentYear,
            seriesFilter: IsStatewideSmSeries,
            onDataPoint: (seriesId, point) =>
            {
                AddStateRow(rowsByKey, seriesId, point);
                return ValueTask.CompletedTask;
            },
            cancellationToken: cancellationToken);
        logger.LogInformation("CES: completed state-level SM flat-file processing, total rows in cache: {Count}", rowsByKey.Count);

        if (!rowsByKey.Keys.Any(k => !k.StartsWith("00:", StringComparison.Ordinal)))
        {
            logger.LogInformation("CES: no state rows from SM flat file, supplementing from BLS API state series");
            await FetchAndMergeStateRowsFromApiAsync(rowsByKey, startYear, currentYear, cancellationToken);
        }

        if (rowsByKey.Count > 0)
        {
            var mergedRows = rowsByKey.Values.ToList();
            logger.LogInformation("CES: upserting {Count} rows (national + states)", mergedRows.Count);
            await UpsertAsync(mergedRows, cancellationToken);
            logger.LogInformation("CES: upsert completed successfully");
            return mergedRows.Count;
        }

        logger.LogInformation("CES: no rows collected, falling back to BLS API after flat-file load returned no data");

        // BLS API allows 50 series per request; send in batches.
        var total = 0;
        foreach (var batch in Series.Chunk(50))
        {
            total += await FetchAndUpsertBatchAsync(batch, startYear, currentYear, cancellationToken);
            await Task.Delay(200, cancellationToken);
        }
        return total;
    }

    private async Task FetchAndMergeStateRowsFromApiAsync(
        Dictionary<string, CesRow> rowsByKey,
        int startYear,
        int endYear,
        CancellationToken cancellationToken)
    {
        var stateSeries = BuildStateSeriesIds();

        foreach (var batch in stateSeries.Chunk(50))
        {
            var body = new Dictionary<string, object>
            {
                ["seriesid"] = batch,
                ["startyear"] = startYear.ToString(),
                ["endyear"] = endYear.ToString(),
            };

            if (!string.IsNullOrWhiteSpace(blsApiKey))
                body["registrationkey"] = blsApiKey;

            using var response = await httpClient.PostAsJsonAsync(
                "https://api.bls.gov/publicAPI/v2/timeseries/data/",
                body,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("CES: state API batch failed with status {Status}", response.StatusCode);
                continue;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var hash = SourceHashService.ComputeSha256Hex(bytes);
            var seriesKey = string.Join(",", batch);
            var sourceUrl = $"https://api.bls.gov/publicAPI/v2/timeseries/data/#series={seriesKey}&start={startYear}&end={endYear}";

            var changed = await sourceHashService.ShouldProcessAsync(
                connectionString,
                "BLS-API-CES-STATE",
                sourceUrl,
                hash,
                bytes.LongLength,
                cancellationToken);

            if (!changed)
                continue;

            var blsResponse = JsonSerializer.Deserialize<BlsResponse>(bytes, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            if (blsResponse?.Results is null)
                continue;

            foreach (var series in blsResponse.Results.Series)
            {
                foreach (var point in series.Data)
                {
                    AddStateRow(rowsByKey, series.SeriesId, point);
                }
            }

            await Task.Delay(200, cancellationToken);
        }
    }

    private static string[] BuildStateSeriesIds()
    {
        var list = new List<string>(StateFipsCodes.Length * 2 * 5);

        foreach (var st in StateFipsCodes)
        {
            foreach (var seasonal in new[] { 'U', 'S' })
            {
                foreach (var dataType in new[] { "01", "02", "03", "06", "11" })
                {
                    list.Add($"SM{seasonal}{st}0000000000000{dataType}");
                }
            }
        }

        return list.ToArray();
    }

    private async Task<int> FetchAndUpsertBatchAsync(
        string[] seriesBatch,
        int startYear,
        int endYear,
        CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, object>
        {
            ["seriesid"] = seriesBatch,
            ["startyear"] = startYear.ToString(),
            ["endyear"] = endYear.ToString(),
        };

        if (!string.IsNullOrWhiteSpace(blsApiKey))
            body["registrationkey"] = blsApiKey;

        logger.LogInformation("CES: requesting {Count} series from BLS API", seriesBatch.Length);

        using var response = await httpClient.PostAsJsonAsync(
            "https://api.bls.gov/publicAPI/v2/timeseries/data/",
            body,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var hash = SourceHashService.ComputeSha256Hex(bytes);
        var endpoint = "https://api.bls.gov/publicAPI/v2/timeseries/data/";
        var seriesKey = string.Join(",", seriesBatch);
        var sourceUrl = $"{endpoint}#series={seriesKey}&start={startYear}&end={endYear}";
        var changed = await sourceHashService.ShouldProcessAsync(
            connectionString,
            "BLS-API-CES",
            sourceUrl,
            hash,
            bytes.LongLength,
            cancellationToken);

        if (!changed)
        {
            logger.LogInformation("CES: fallback batch unchanged, skipping series batch");
            return 0;
        }

        var blsResponse = JsonSerializer.Deserialize<BlsResponse>(bytes, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        if (blsResponse?.Results is null)
        {
            logger.LogWarning("CES: BLS response contained no results");
            return 0;
        }

        var rows = BuildNationalRows(blsResponse.Results.Series);
        logger.LogInformation("CES: upserting {Count} rows", rows.Count);
        await UpsertAsync(rows, cancellationToken);
        return rows.Count;
    }

    private static bool IsStatewideSmSeries(string seriesId)
    {
        // SM series are 20 characters: SMS + StateCode(2) + Area/Industry(13) + DataType(2) = 20
        if (string.IsNullOrWhiteSpace(seriesId) || seriesId.Length != 20)
            return false;

        if (!seriesId.StartsWith("SM", StringComparison.OrdinalIgnoreCase))
            return false;

        var seasonal = char.ToUpperInvariant(seriesId[2]);
        if (seasonal is not ('S' or 'U'))
            return false;

        // Keep statewide rows only for deterministic WID state geography mapping.
        // SM series format: SMS[StateCode][Area/Industry13Chars][DataType2Digits]
        // Position 3-4: state FIPS code (00 = national, 01 = Alabama, etc.)
        // Positions 5-17: area/industry codes (13 chars, all 0s for statewide)
        // Positions 18-19: data type code (last 2 digits)
        
        // Verify area code is all zeros (statewide only)
        var areaCode = seriesId[5..18]; // 13-character area code field
        if (!areaCode.All(c => c == '0'))
            return false; // Not statewide (should be all zeros)

        // Extract data type from last 2 characters
        var dataTypeCode = seriesId[18..20]; // Last 2 characters = data type code
        
        return dataTypeCode is "01" or "02" or "03" or "06" or "11";
    }

    private static void AddStateRow(Dictionary<string, CesRow> rowsByKey, string seriesId, BlsDataPoint point)
    {
        if (!BlsPeriodMapper.TryMap(point.Period, out var periodType, out var period))
            return;

        if (!BlsNumericParser.TryParseDecimal(point.Value, out var value))
            return;

        // SM series format: SMS[StateCode][Area13Zeros][DataType2Digits]
        var stFips = seriesId[3..5]; // State FIPS code (positions 3-4)
        var industryCode = seriesId[5..18]; // Area/industry field (positions 5-17, 13 chars, all zeros for statewide)
        var dataTypeCode = seriesId[18..20]; // Data type code (positions 18-19, last 2 characters)
        var adjusted = char.ToUpperInvariant(seriesId[2]) == 'S' ? "1" : "0";
        var isPrelim = point.Footnotes.Any(f => f.Code == "P");

        var key =
            $"{stFips}:01:0:{stFips.PadLeft(6, '0')}:{point.Year}:{periodType}:{period}:{SeriesCodeType}:{industryCode}:{adjusted}";

        if (!rowsByKey.TryGetValue(key, out var row))
        {
            row = new CesRow
            {
                StFips = stFips,
                AreaType = "01",
                AreaTypeVersion = "0",
                Area = stFips.PadLeft(6, '0'),
                PeriodYear = point.Year,
                PeriodType = periodType,
                Period = period,
                Adjusted = adjusted,
                SeriesCodeType = SeriesCodeType,
                SeriesCode = industryCode,
                Prelim = isPrelim ? "1" : "0",
            };
        }
        else if (isPrelim)
        {
            row = row with { Prelim = "1" };
        }

        switch (dataTypeCode)
        {
            case "01":
                row = row with { EmpCes = (long?)value };
                break;
            case "06":
                row = row with { EmpProductionWorkers = (long?)value };
                break;
            case "02":
                row = row with { HoursPerWeek = value };
                break;
            case "03":
                row = row with { EarningsPerHour = value };
                break;
            case "11":
                row = row with { EarningsPerWeek = value };
                break;
        }

        rowsByKey[key] = row;
    }

    private static List<CesRow> BuildNationalRows(IEnumerable<BlsSeries> allSeries)
    {
        var rows = new List<CesRow>();
        foreach (var series in allSeries)
        {
            var industryCode = series.SeriesId.Length >= 11
                ? series.SeriesId[3..11]
                : series.SeriesId;

            foreach (var point in series.Data)
            {
                if (!BlsPeriodMapper.TryMap(point.Period, out var periodType, out var period))
                    continue;

                var isPrelim = point.Footnotes.Any(f => f.Code == "P");
                var value = BlsNumericParser.TryParseDecimal(point.Value, out var v) ? v : (decimal?)null;

                rows.Add(new CesRow
                {
                    PeriodYear = point.Year,
                    PeriodType = periodType,
                    Period = period,
                    Adjusted = "1", // national CES series are SA
                    SeriesCodeType = SeriesCodeType,
                    SeriesCode = industryCode,
                    EmpCes = (long?)value,
                    Prelim = isPrelim ? "1" : "0",
                });
            }
        }
        return rows;
    }

    private static void MergeRows(Dictionary<string, CesRow> target, IEnumerable<CesRow> source)
    {
        foreach (var row in source)
        {
            var key =
                $"{row.StFips}:{row.AreaType}:{row.AreaTypeVersion}:{row.Area}:{row.PeriodYear}:{row.PeriodType}:{row.Period}:{row.SeriesCodeType}:{row.SeriesCode}:{row.Adjusted}";
            target[key] = row;
        }
    }

    private async Task UpsertAsync(List<CesRow> rows, CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);

        const int batchSize = 500;
        foreach (var chunk in rows.Chunk(batchSize))
        {
            var batch = new NpgsqlBatch(conn, tx);

            foreach (var row in chunk)
            {
                var cmd = new NpgsqlBatchCommand(@"
                    INSERT INTO ces
                        (stfips, areatype, areatypeversion, area, periodyear, periodtype, period, adjusted,
                         seriescodetype, seriescode, empces, empproductionworkers, hoursperweek,
                         earningsperweek, earningsperhour, avgweeklyearningspctchange,
                         supprecord, supphoursearnings, suppprodworkers, prelim)
                    VALUES
                        (@stfips, @areatype, @areatypeversion, @area, @periodyear, @periodtype, @period, @adjusted,
                         @seriescodetype, @seriescode, @empces, @empproductionworkers, @hoursperweek,
                         @earningsperweek, @earningsperhour, @avgweeklyearningspctchange,
                         @supprecord, @supphoursearnings, @suppprodworkers, @prelim)
                    ON CONFLICT (stfips, areatype, areatypeversion, area, periodyear, periodtype, period,
                                 seriescodetype, seriescode, adjusted)
                    DO UPDATE SET
                        empces = EXCLUDED.empces,
                        empproductionworkers = EXCLUDED.empproductionworkers,
                        hoursperweek = EXCLUDED.hoursperweek,
                        earningsperweek = EXCLUDED.earningsperweek,
                        earningsperhour = EXCLUDED.earningsperhour,
                        avgweeklyearningspctchange = EXCLUDED.avgweeklyearningspctchange,
                        supprecord = EXCLUDED.supprecord,
                        supphoursearnings = EXCLUDED.supphoursearnings,
                        suppprodworkers = EXCLUDED.suppprodworkers,
                        prelim = EXCLUDED.prelim");

                cmd.Parameters.AddWithValue("stfips", row.StFips);
                cmd.Parameters.AddWithValue("areatype", row.AreaType);
                cmd.Parameters.AddWithValue("areatypeversion", row.AreaTypeVersion);
                cmd.Parameters.AddWithValue("area", row.Area);
                cmd.Parameters.AddWithValue("periodyear", row.PeriodYear);
                cmd.Parameters.AddWithValue("periodtype", row.PeriodType);
                cmd.Parameters.AddWithValue("period", row.Period);
                cmd.Parameters.AddWithValue("adjusted", row.Adjusted);
                cmd.Parameters.AddWithValue("seriescodetype", row.SeriesCodeType);
                cmd.Parameters.AddWithValue("seriescode", row.SeriesCode);
                cmd.Parameters.AddWithValue("empces", (object?)row.EmpCes ?? DBNull.Value);
                cmd.Parameters.AddWithValue("empproductionworkers", (object?)row.EmpProductionWorkers ?? DBNull.Value);
                cmd.Parameters.AddWithValue("hoursperweek", (object?)row.HoursPerWeek ?? DBNull.Value);
                cmd.Parameters.AddWithValue("earningsperweek", (object?)row.EarningsPerWeek ?? DBNull.Value);
                cmd.Parameters.AddWithValue("earningsperhour", (object?)row.EarningsPerHour ?? DBNull.Value);
                cmd.Parameters.AddWithValue("avgweeklyearningspctchange", (object?)row.AvgWeeklyEarningsPctChange ?? DBNull.Value);
                cmd.Parameters.AddWithValue("supprecord", row.SuppRecord);
                cmd.Parameters.AddWithValue("supphoursearnings", row.SuppHoursEarnings);
                cmd.Parameters.AddWithValue("suppprodworkers", row.SuppProdWorkers);
                cmd.Parameters.AddWithValue("prelim", row.Prelim);

                batch.BatchCommands.Add(cmd);
            }

            await batch.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }
}

