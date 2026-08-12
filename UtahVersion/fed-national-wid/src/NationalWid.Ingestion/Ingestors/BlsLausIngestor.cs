using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Npgsql;
using NationalWid.Ingestion.Models;
using NationalWid.Ingestion.Services;

namespace NationalWid.Ingestion.Ingestors;

/// <summary>
/// Fetches national + state LAUS series and upserts into the
/// <c>laborforce</c> table.
/// </summary>
public sealed class BlsLausIngestor(
    HttpClient httpClient,
    string? blsApiKey,
    string connectionString,
    SourceHashService sourceHashService,
    ILogger logger)
{
    // National-level LAUS series (US, seasonally adjusted).
    private static readonly string[] Series =
    [
        "LNS11000000", // Civilian labor force
        "LNS12000000", // Civilian employment
        "LNS13000000", // Civilian unemployment
        "LNS14000000", // Unemployment rate
    ];

    // 50 states + DC (2-digit FIPS).
    private static readonly string[] StateFipsCodes =
    [
        "01", "02", "04", "05", "06", "08", "09", "10", "11", "12", "13", "15", "16", "17", "18", "19",
        "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "30", "31", "32", "33", "34", "35",
        "36", "37", "38", "39", "40", "41", "42", "44", "45", "46", "47", "48", "49", "50", "51", "53",
        "54", "55", "56",
    ];

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var currentYear = DateTime.UtcNow.Year;
        var startYear = currentYear - 20;
        var rowsByKey = new Dictionary<string, LaborForceRow>(StringComparer.Ordinal);

        var flatFileService = new BlsFlatFileService(httpClient, logger, sourceHashService, connectionString);

        // National CPS series (historical behavior) to keep U.S. rollup populated.
        var flatSeries = await flatFileService.TryLoadSeriesAsync(
            dataset: "ln",
            dataFile: "ln.data.1.AllData",
            seriesIds: Series,
            startYear: startYear,
            endYear: currentYear,
            cancellationToken: cancellationToken);

        if (flatSeries.Count > 0)
        {
            MergeRows(rowsByKey, BuildNationalRows(flatSeries));
            logger.LogInformation("LAUS: loaded {Count} national rows from BLS ln flat file", rowsByKey.Count);
        }

        // All-state unadjusted and seasonally adjusted LAUS data.
        foreach (var dataFile in new[] { "la.data.2.AllStatesU", "la.data.3.AllStatesS" })
        {
            await flatFileService.ProcessDataFileAsync(
                dataset: "la",
                dataFile: dataFile,
                startYear: startYear,
                endYear: currentYear,
                seriesFilter: IsStateLausSeries,
                onDataPoint: (seriesId, point) =>
                {
                    AddStateRow(rowsByKey, seriesId, point);
                    return ValueTask.CompletedTask;
                },
                cancellationToken: cancellationToken);
        }

            // Some LAUS flat-file variants occasionally publish under different names.
            // If state rows are still missing, query canonical state series IDs via BLS API.
            if (!rowsByKey.Keys.Any(k => !k.StartsWith("00:", StringComparison.Ordinal)))
            {
                logger.LogInformation("LAUS: no state rows from flat files, supplementing from BLS API state series");
                await FetchAndMergeStateRowsFromApiAsync(rowsByKey, startYear, currentYear, cancellationToken);
            }

        if (rowsByKey.Count == 0)
        {
            // Final safety net if both flat-file paths are unavailable.
            logger.LogInformation("LAUS: falling back to BLS API after flat-file loads returned no data");

            var body = new Dictionary<string, object>
            {
                ["seriesid"] = Series,
                ["startyear"] = startYear.ToString(),
                ["endyear"] = currentYear.ToString(),
            };

            if (!string.IsNullOrWhiteSpace(blsApiKey))
                body["registrationkey"] = blsApiKey;

            logger.LogInformation("LAUS: requesting {Count} fallback national series from BLS API", Series.Length);

            await Task.Delay(100, cancellationToken); // respect rate limit
            using var response = await httpClient.PostAsJsonAsync(
                "https://api.bls.gov/publicAPI/v2/timeseries/data/",
                body,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var hash = SourceHashService.ComputeSha256Hex(bytes);
            var apiUrl = "https://api.bls.gov/publicAPI/v2/timeseries/data/";
            var changed = await sourceHashService.ShouldProcessAsync(
                connectionString,
                "BLS-API-LAUS",
                apiUrl,
                hash,
                bytes.LongLength,
                cancellationToken);
            if (!changed)
            {
                logger.LogInformation("LAUS: API fallback response unchanged, skipping fallback merge");
            }

            var blsResponse = JsonSerializer.Deserialize<BlsResponse>(bytes, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });

            if (changed && blsResponse?.Results is not null)
            {
                MergeRows(rowsByKey, BuildNationalRows(blsResponse.Results.Series));
            }
        }

        var rows = rowsByKey.Values.ToList();
        logger.LogInformation("LAUS: upserting {Count} rows (national + states)", rows.Count);
        await UpsertAsync(rows, cancellationToken);
        return rows.Count;
    }

    private async Task FetchAndMergeStateRowsFromApiAsync(
        Dictionary<string, LaborForceRow> rowsByKey,
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
                logger.LogWarning("LAUS: state API batch failed with status {Status}", response.StatusCode);
                continue;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var hash = SourceHashService.ComputeSha256Hex(bytes);
            var seriesKey = string.Join(",", batch);
            var sourceUrl = $"https://api.bls.gov/publicAPI/v2/timeseries/data/#series={seriesKey}&start={startYear}&end={endYear}";

            var changed = await sourceHashService.ShouldProcessAsync(
                connectionString,
                "BLS-API-LAUS-STATE",
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
        var list = new List<string>(StateFipsCodes.Length * 2 * 4);

        foreach (var st in StateFipsCodes)
        {
            foreach (var seasonal in new[] { 'U', 'S' })
            {
                foreach (var measure in new[] { "03", "04", "05", "06" })
                {
                    list.Add($"LA{seasonal}ST{st}00000000000{measure}");
                }
            }
        }

        return list.ToArray();
    }

    private static bool IsStateLausSeries(string seriesId)
    {
        if (string.IsNullOrWhiteSpace(seriesId) || seriesId.Length < 20)
            return false;

        if (!seriesId.StartsWith("LA", StringComparison.OrdinalIgnoreCase))
            return false;

        var seasonal = char.ToUpperInvariant(seriesId[2]);
        if (seasonal is not ('S' or 'U'))
            return false;

        var areaCode = seriesId[3..18];
        if (!areaCode.StartsWith("ST", StringComparison.OrdinalIgnoreCase))
            return false;

        var measureCode = seriesId[18..20];
        return measureCode is "03" or "04" or "05" or "06";
    }

    private static void AddStateRow(Dictionary<string, LaborForceRow> rowsByKey, string seriesId, BlsDataPoint point)
    {
        if (!BlsPeriodMapper.TryMap(point.Period, out var periodType, out var period))
            return;

        var areaCode = seriesId[3..18];
        var stFips = areaCode[2..4];
        var measureCode = seriesId[18..20];
        var adjusted = char.ToUpperInvariant(seriesId[2]) == 'S' ? "1" : "0";

        if (!BlsNumericParser.TryParseDecimal(point.Value, out var value))
            return;

        var isPrelim = point.Footnotes.Any(f => f.Code == "P");
        var rowKey = $"{stFips}:01:0:{stFips.PadLeft(6, '0')}:{point.Year}:{periodType}:{period}:{adjusted}";

        if (!rowsByKey.TryGetValue(rowKey, out var row))
        {
            row = new LaborForceRow
            {
                StFips = stFips,
                AreaType = "01",
                AreaTypeVersion = "0",
                Area = stFips.PadLeft(6, '0'),
                PeriodYear = point.Year,
                PeriodType = periodType,
                Period = period,
                Adjusted = adjusted,
                Prelim = isPrelim ? "1" : "0",
            };
        }
        else if (isPrelim)
        {
            row = row with { Prelim = "1" };
        }

        switch (measureCode)
        {
            case "03":
                row = row with { UnempRate = value };
                break;
            case "04":
                row = row with { Unemployed = (long?)value };
                break;
            case "05":
                row = row with { Employed = (long?)value };
                break;
            case "06":
                row = row with { LaborForce = (long?)value };
                break;
        }

        rowsByKey[rowKey] = row;
    }

    private static List<LaborForceRow> BuildNationalRows(IEnumerable<BlsSeries> allSeries)
    {
        var rowsByPeriod = new Dictionary<string, LaborForceRow>(StringComparer.Ordinal);

        foreach (var series in allSeries)
        {
            foreach (var point in series.Data)
            {
                if (!BlsPeriodMapper.TryMap(point.Period, out var periodType, out var period))
                    continue;

                var isPrelim = point.Footnotes.Any(f => f.Code == "P");
                var value = BlsNumericParser.TryParseDecimal(point.Value, out var v) ? v : (decimal?)null;
                var key = $"{point.Year}:{periodType}:{period}";

                if (!rowsByPeriod.TryGetValue(key, out var row))
                {
                    row = new LaborForceRow
                    {
                        PeriodYear = point.Year,
                        PeriodType = periodType,
                        Period = period,
                        Adjusted = "1", // LAUS national series are seasonally adjusted
                        Prelim = isPrelim ? "1" : "0",
                    };
                }
                else if (isPrelim)
                {
                    row = row with { Prelim = "1" };
                }

                // Map each series to its column.
                switch (series.SeriesId)
                {
                    case "LNS11000000":
                        row = row with { LaborForce = (long?)value };
                        break;
                    case "LNS12000000":
                        row = row with { Employed = (long?)value };
                        break;
                    case "LNS13000000":
                        row = row with { Unemployed = (long?)value };
                        break;
                    case "LNS14000000":
                        row = row with { UnempRate = value };
                        break;
                }

                rowsByPeriod[key] = row;
            }
        }

        return rowsByPeriod.Values.ToList();
    }

    private static void MergeRows(Dictionary<string, LaborForceRow> target, IEnumerable<LaborForceRow> source)
    {
        foreach (var row in source)
        {
            var key =
                $"{row.StFips}:{row.AreaType}:{row.AreaTypeVersion}:{row.Area}:{row.PeriodYear}:{row.PeriodType}:{row.Period}:{row.Adjusted}";
            target[key] = row;
        }
    }

    private async Task UpsertAsync(List<LaborForceRow> rows, CancellationToken cancellationToken)
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
                    INSERT INTO laborforce
                        (stfips, areatype, areatypeversion, area, periodyear, periodtype, period, adjusted,
                         laborforce, employed, unemployed, unemprate, supprecord, supprate, prelim)
                    VALUES
                        (@stfips, @areatype, @areatypeversion, @area, @periodyear, @periodtype, @period, @adjusted,
                         @laborforce, @employed, @unemployed, @unemprate, @supprecord, @supprate, @prelim)
                    ON CONFLICT (stfips, areatype, areatypeversion, area, periodyear, periodtype, period, adjusted)
                    DO UPDATE SET
                        laborforce = EXCLUDED.laborforce,
                        employed = EXCLUDED.employed,
                        unemployed = EXCLUDED.unemployed,
                        unemprate = EXCLUDED.unemprate,
                        supprecord = EXCLUDED.supprecord,
                        supprate = EXCLUDED.supprate,
                        prelim = EXCLUDED.prelim");

                cmd.Parameters.AddWithValue("stfips", row.StFips);
                cmd.Parameters.AddWithValue("areatype", row.AreaType);
                cmd.Parameters.AddWithValue("areatypeversion", row.AreaTypeVersion);
                cmd.Parameters.AddWithValue("area", row.Area);
                cmd.Parameters.AddWithValue("periodyear", row.PeriodYear);
                cmd.Parameters.AddWithValue("periodtype", row.PeriodType);
                cmd.Parameters.AddWithValue("period", row.Period);
                cmd.Parameters.AddWithValue("adjusted", row.Adjusted);
                cmd.Parameters.AddWithValue("laborforce", (object?)row.LaborForce ?? DBNull.Value);
                cmd.Parameters.AddWithValue("employed", (object?)row.Employed ?? DBNull.Value);
                cmd.Parameters.AddWithValue("unemployed", (object?)row.Unemployed ?? DBNull.Value);
                cmd.Parameters.AddWithValue("unemprate", (object?)row.UnempRate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("supprecord", row.SuppRecord);
                cmd.Parameters.AddWithValue("supprate", row.SuppRate);
                cmd.Parameters.AddWithValue("prelim", row.Prelim);

                batch.BatchCommands.Add(cmd);
            }

            await batch.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }
}

