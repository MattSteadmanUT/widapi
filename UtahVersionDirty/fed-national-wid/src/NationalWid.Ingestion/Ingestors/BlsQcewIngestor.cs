using System.Globalization;
using Microsoft.Extensions.Logging;
using Npgsql;
using NationalWid.Ingestion.Models;
using NationalWid.Ingestion.Services;

namespace NationalWid.Ingestion.Ingestors;

/// <summary>
/// Fetches QCEW data from BLS CSV slices and upserts into the <c>industry</c> table.
/// Uses industry slice URLs: https://data.bls.gov/cew/data/api/{year}/{quarter}/industry/10.csv
/// and filters to national + state-level all-establishment-size records.
/// </summary>
public sealed class BlsQcewIngestor(
    HttpClient httpClient,
    string connectionString,
    SourceHashService sourceHashService,
    ILogger logger)
{
    // Fetch last 5 years of quarterly data.
    private const int YearsBack = 5;

    private readonly Dictionary<string, (string StFips, string AreaType, string Area)> _countyMap =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, (string StFips, string AreaType, string Area)> _msaMap =
        new(StringComparer.OrdinalIgnoreCase);

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        await LoadGeographyMapsAsync(cancellationToken);

        var total = 0;
        var currentYear = DateTime.UtcNow.Year;
        var currentQuarter = (DateTime.UtcNow.Month - 1) / 3 + 1;

        for (var year = currentYear - YearsBack; year <= currentYear; year++)
        {
            var maxQ = (year == currentYear) ? currentQuarter : 4;
            for (var q = 1; q <= maxQ; q++)
            {
                total += await FetchQuarterAsync(year, q, cancellationToken);
                await Task.Delay(200, cancellationToken);
            }
        }

        return total;
    }

    private async Task<int> FetchQuarterAsync(int year, int quarter, CancellationToken cancellationToken)
    {
        var url = $"https://data.bls.gov/cew/data/api/{year}/{quarter}/industry/10.csv";
        logger.LogInformation("QCEW: fetching {Year} Q{Quarter}", year, quarter);

        using var response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            if ((int)response.StatusCode is 404 or 204)
            {
                logger.LogInformation(
                    "QCEW: source unavailable for {Year} Q{Quarter} ({Status}); continuing",
                    year,
                    quarter,
                    response.StatusCode);
            }
            else
            {
                logger.LogWarning(
                    "QCEW: request failed for {Year} Q{Quarter} with {Status}; continuing",
                    year,
                    quarter,
                    response.StatusCode);
            }
            return 0;
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var hash = SourceHashService.ComputeSha256Hex(bytes);
        var changed = await sourceHashService.ShouldProcessAsync(
            connectionString,
            "BLS-QCEW",
            url,
            hash,
            bytes.LongLength,
            cancellationToken);

        if (!changed)
        {
            return 0;
        }

        var csv = System.Text.Encoding.UTF8.GetString(bytes);
        var rows = BuildRowsFromCsv(csv);
        if (rows.Count == 0)
            return 0;

        logger.LogInformation("QCEW: upserting {Count} rows for {Year} Q{Quarter}", rows.Count, year, quarter);
        await UpsertAsync(rows, cancellationToken);
        return rows.Count;
    }

    private async Task LoadGeographyMapsAsync(CancellationToken cancellationToken)
    {
        if (_countyMap.Count > 0 || _msaMap.Count > 0)
            return;

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        var cmd = new NpgsqlCommand(@"
            SELECT stfips, areatype, area
            FROM geographies
            WHERE areatype IN ('04', '21', '31');", conn);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var stFips = reader.GetString(0);
            var areaType = reader.GetString(1);
            var area = reader.GetString(2);

            if (areaType == "04" && area.Length >= 5)
            {
                // WID county area code is "{stFips}{county3}00" so area[..5] = 5-digit FIPS.
                var countyCode = area[..5];
                if (!_countyMap.ContainsKey(countyCode))
                {
                    _countyMap[countyCode] = (stFips, areaType, area);
                }
            }

            if ((areaType == "21" || areaType == "31") && area.Length >= 4)
            {
                // MSA area code in WID is the 6-digit BLS CBSA code padded to 7.
                // QCEW uses C#### (4-digit) for MSAs; map by the last 4 digits of the CBSA code.
                if (area.Length >= 5)
                {
                    var msaNumeric = area.TrimStart('0');
                    if (msaNumeric.Length > 0)
                    {
                        // Index by the rightmost 4 digits (the CBSA core code).
                        var msaKey = msaNumeric.Length >= 4
                            ? msaNumeric[^4..]
                            : msaNumeric.PadLeft(4, '0');
                        if (!_msaMap.ContainsKey(msaKey))
                            _msaMap[msaKey] = (stFips, areaType == "31" ? "31" : "21", area);
                    }
                }
            }
        }

        logger.LogInformation("QCEW: geography maps loaded (county={CountyCount}, msa={MsaCount})", _countyMap.Count, _msaMap.Count);
    }

    private List<IndustryRow> BuildRowsFromCsv(string csv)
    {
        var rowsByKey = new Dictionary<string, IndustryRow>(StringComparer.Ordinal);
        using var reader = new StringReader(csv);

        // Skip header.
        _ = reader.ReadLine();

        while (reader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var cols = ParseCsvLine(line);
            if (cols.Count < 16)
                continue;

            var areaFips = cols[0].Trim();
            var ownCode = cols[1].Trim();
            var industryCode = cols[2].Trim();
            var sizeCode = cols[4].Trim();
            var year = cols[5].Trim();
            var quarter = cols[6].Trim();
            var disclosureCode = cols[7].Trim();

            // Restrict to all-establishment-size records for national and state-wide areas.
            if (sizeCode != "0")
                continue;

            if (!TryResolveArea(areaFips, out var stFips, out var areaType, out var area))
                continue;

            if (quarter.Length != 1 || quarter[0] is < '1' or > '4')
                continue;

            var period = quarter;
            var key =
                $"{stFips}:{areaType}:0:{area}:{year}:02:{period}:{ownCode}:{industryCode}";

            rowsByKey[key] = new IndustryRow
            {
                StFips = stFips,
                AreaType = areaType,
                AreaTypeVersion = "0",
                Area = area,
                PeriodYear = year,
                PeriodType = "02", // quarterly
                Period = period,
                Ownership = string.IsNullOrEmpty(ownCode) ? "0" : ownCode,
                IndCode = string.IsNullOrEmpty(industryCode) ? "10" : industryCode,
                AvgMonthlyEmp = ParseNullableDecimal(cols[9], cols[10], cols[11]),
                TotalWages = ParseNullableDecimal(cols[12]),
                TaxableWages = ParseNullableDecimal(cols[13]),
                Contributions = ParseNullableDecimal(cols[14]),
                WeeklyWage = ParseNullableDecimal(cols[15]),
                SuppRecord = disclosureCode.Equals("N", StringComparison.OrdinalIgnoreCase) ? "1" : "0",
            };
        }

        return rowsByKey.Values.ToList();
    }

    private bool TryResolveArea(string areaFips, out string stFips, out string areaType, out string area)
    {
        stFips = "";
        areaType = "";
        area = "";

        if (string.Equals(areaFips, "US000", StringComparison.OrdinalIgnoreCase))
        {
            stFips = "00";
            areaType = "00";
            area = "000000";
            return true;
        }

        if (areaFips.Length == 5 && areaFips.All(char.IsDigit) && areaFips.EndsWith("000", StringComparison.Ordinal))
        {
            stFips = areaFips[..2];
            areaType = "01";
            area = stFips.PadLeft(6, '0');
            return true;
        }

        // County-level QCEW areas (e.g., 01001): area = county suffix (last 3 digits) padded to 6.
        if (areaFips.Length == 5 && areaFips.All(char.IsDigit) && !areaFips.EndsWith("000", StringComparison.Ordinal))
        {
            stFips = areaFips[..2];
            areaType = "04";
            area = areaFips[2..].PadLeft(6, '0'); // county suffix (3 digits) padded to 6
            return true;
        }

        // MSA-level QCEW areas commonly appear as C#### and are resolved via geographies.
        if (areaFips.Length == 5 && areaFips[0] == 'C')
        {
            var code = areaFips[1..];
            if (_msaMap.TryGetValue(code, out var msa))
            {
                stFips = msa.StFips;
                areaType = msa.AreaType;
                area = msa.Area;
                return true;
            }
        }

        return false;
    }

    private async Task UpsertAsync(List<IndustryRow> rows, CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        foreach (var batchRows in rows.Chunk(500))
        {
            await using var tx = await conn.BeginTransactionAsync(cancellationToken);
            await using var batch = new NpgsqlBatch(conn, tx);

            foreach (var row in batchRows)
            {
                var cmd = new NpgsqlBatchCommand(@"
                    INSERT INTO industry
                        (stfips, areatype, areatypeversion, area, periodyear, periodtype, period,
                         ownership, indcodetype, indcode,
                         avgmonthlyemp, totalwages, taxablewages, contributions, weeklywage,
                         empcount, highempq, lowempq, supprecord, prelim)
                    VALUES
                        (@stfips, @areatype, @areatypeversion, @area, @periodyear, @periodtype, @period,
                         @ownership, @indcodetype, @indcode,
                         @avgmonthlyemp, @totalwages, @taxablewages, @uicontributions, @weeklywage,
                         @empcount, @highempq, @lowempq, @supprecord, @prelim)
                    ON CONFLICT (stfips, areatype, areatypeversion, area, periodyear, periodtype, period,
                                 ownership, codetype, indcode)
                    DO UPDATE SET
                        avgmonthlyemp = EXCLUDED.avgmonthlyemp,
                        totalwages = EXCLUDED.totalwages,
                        taxablewages = EXCLUDED.taxablewages,
                        contributions = EXCLUDED.UIContributions,
                        weeklywage = EXCLUDED.weeklywage,
                        empcount = EXCLUDED.empcount,
                        highempq = EXCLUDED.highempq,
                        lowempq = EXCLUDED.lowempq,
                        supprecord = EXCLUDED.SuppRecord,
                        prelim = EXCLUDED.prelim");

                cmd.Parameters.AddWithValue("stfips", row.StFips);
                cmd.Parameters.AddWithValue("areatype", row.AreaType);
                cmd.Parameters.AddWithValue("areatypeversion", row.AreaTypeVersion);
                cmd.Parameters.AddWithValue("area", row.Area);
                cmd.Parameters.AddWithValue("periodyear", row.PeriodYear);
                cmd.Parameters.AddWithValue("periodtype", row.PeriodType);
                cmd.Parameters.AddWithValue("period", row.Period);
                cmd.Parameters.AddWithValue("ownership", row.Ownership);
                cmd.Parameters.AddWithValue("indcodetype", row.IndCodeType);
                cmd.Parameters.AddWithValue("indcode", row.IndCode);
                cmd.Parameters.AddWithValue("avgmonthlyemp", (object?)row.AvgMonthlyEmp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("totalwages", (object?)row.TotalWages ?? DBNull.Value);
                cmd.Parameters.AddWithValue("taxablewages", (object?)row.TaxableWages ?? DBNull.Value);
                cmd.Parameters.AddWithValue("uicontributions", (object?)row.Contributions ?? DBNull.Value);
                cmd.Parameters.AddWithValue("weeklywage", (object?)row.WeeklyWage ?? DBNull.Value);
                cmd.Parameters.AddWithValue("empcount", (object?)row.EmpCount ?? DBNull.Value);
                cmd.Parameters.AddWithValue("highempq", (object?)row.HighEmpQ ?? DBNull.Value);
                cmd.Parameters.AddWithValue("lowempq", (object?)row.LowEmpQ ?? DBNull.Value);
                cmd.Parameters.AddWithValue("supprecord", row.SuppRecord);
                cmd.Parameters.AddWithValue("prelim", row.Prelim);

                batch.BatchCommands.Add(cmd);
            }

            await batch.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
    }

    private static decimal? ParseNullableDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static decimal? ParseNullableDecimal(string? month1, string? month2, string? month3)
    {
        var m1 = ParseNullableDecimal(month1);
        var m2 = ParseNullableDecimal(month2);
        var m3 = ParseNullableDecimal(month3);
        if (m1 is null || m2 is null || m3 is null)
            return null;
        return (m1.Value + m2.Value + m3.Value) / 3m;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        values.Add(current.ToString());
        return values;
    }
}



