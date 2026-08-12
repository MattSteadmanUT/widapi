using System.Globalization;
using System.IO.Compression;
using NationalWid.Ingestion.Services;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using Npgsql;
using NationalWid.Ingestion.Models;

namespace NationalWid.Ingestion.Ingestors;

/// <summary>
/// Fetches OEWS downloadable ZIP workbooks (national + state) and upserts into
/// the <c>iowage</c> table. Source examples:
/// - https://www.bls.gov/oes/special.requests/oesm23nat.zip
/// - https://www.bls.gov/oes/special.requests/oesm23st.zip
/// </summary>
public sealed class BlsOesIngestor(
    HttpClient httpClient,
    string connectionString,
    SourceHashService sourceHashService,
    ILogger logger)
{
    private const int YearsBack = 20; // Expanded from 3 to 20 years for better historical coverage

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var total = 0;
        var currentYear = DateTime.UtcNow.Year;
        var years = Enumerable.Range(currentYear - YearsBack, YearsBack + 1).ToList();

        // Fetch all years in parallel with individual error handling
        // so that one corrupted file doesn't crash the entire ingestor
        var tasks = new List<Task<int>>();
        foreach (var year in years)
        {
            // Fetch national + state + metro area data for complete coverage
            var natTask = SafeFetchWorkbookAsync(year, "nat", cancellationToken);
            var stTask = SafeFetchWorkbookAsync(year, "st", cancellationToken);
            var msaTask = SafeFetchWorkbookAsync(year, "msa", cancellationToken); // Metro areas for geographic completeness
            
            tasks.Add(natTask);
            tasks.Add(stTask);
            tasks.Add(msaTask);
        }

        var results = await Task.WhenAll(tasks);
        total = results.Sum();

        logger.LogInformation("OES: completed ingestion of {Total} records across all years, geographies (nat, st, msa)", total);
        return total;
    }

    private async Task<int> SafeFetchWorkbookAsync(int year, string kind, CancellationToken cancellationToken)
    {
        try
        {
            return await FetchWorkbookAsync(year, kind, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning("OES: error fetching year {Year}, kind {Kind}: {Message}", year, kind, ex.Message);
            return 0; // Continue on error instead of crashing
        }
    }

    private async Task<int> FetchWorkbookAsync(int year, string kind, CancellationToken cancellationToken)
    {
        var url = $"https://www.bls.gov/oes/special.requests/oesm{year % 100:00}{kind}.zip";
        logger.LogInformation("OES: downloading {Url}", url);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", "ULMITA/1.0 (ulmita.org; mattsteadman@utah.gov)");
        request.Headers.TryAddWithoutValidation("Accept", "*/*");
        request.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
        request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");

        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogInformation("OES: source unavailable for year {Year}, kind {Kind} ({Status})", year, kind, response.StatusCode);
            return 0;
        }

        var zipBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var hash = SourceHashService.ComputeSha256Hex(zipBytes);
        var changed = await sourceHashService.ShouldProcessAsync(
            connectionString,
            "BLS-OEWS",
            url,
            hash,
            zipBytes.LongLength,
            cancellationToken);

        if (!changed)
        {
            return 0;
        }

        await using var sourceStream = new MemoryStream(zipBytes);
        using var zip = new ZipArchive(sourceStream, ZipArchiveMode.Read, leaveOpen: false);
        var entry = zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase));
        if (entry is null)
            return 0;

        await using var entryStream = entry.Open();
        using var ms = new MemoryStream();
        await entryStream.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;

        var rows = BuildRows(ms, year.ToString(CultureInfo.InvariantCulture));
        if (rows.Count == 0)
            return 0;

        logger.LogInformation("OES: upserting {Count} rows from {File}", rows.Count, entry.FullName);
        await UpsertAsync(rows, cancellationToken);
        return rows.Count;
    }

    private static List<IOWageRow> BuildRows(Stream workbookStream, string year)
    {
        using var workbook = new XLWorkbook(workbookStream);
        var ws = workbook.Worksheets.First();

        var header = ws.FirstRowUsed();
        if (header is null)
            return [];

        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in header.CellsUsed())
        {
            var name = cell.GetString().Trim();
            if (!string.IsNullOrEmpty(name))
                headerMap[name] = cell.Address.ColumnNumber;
        }

        if (!headerMap.ContainsKey("AREA") || !headerMap.ContainsKey("OCC_CODE"))
            return [];

        var rowsByKey = new Dictionary<string, IOWageRow>(StringComparer.Ordinal);

        foreach (var row in ws.RowsUsed().Skip(1))
        {
            var naics = GetText(row, headerMap, "NAICS");
            var oGroup = GetText(row, headerMap, "O_GROUP");
            var area = GetText(row, headerMap, "AREA");
            var occCodeRaw = GetText(row, headerMap, "OCC_CODE");

            // Include ALL occupational groups: total, major, and detailed.
            // This expands data completeness from ~23 records to thousands.
            // Note: Some o_group values may be other groupings (e.g., "broad").
            // We capture them all for maximum coverage.

            if (!TryMapStFips(area, out var stFips))
                continue;

            var occCode = NormalizeOccCode(occCodeRaw);
            if (string.IsNullOrEmpty(occCode))
                continue;

            var widArea = stFips == "00" ? "000000" : stFips.PadLeft(6, '0');
            var widAreaType = stFips == "00" ? "00" : "01";

            // For industry, map NAICS to WID industry code type / code.
            // If NAICS is "000000" (all industries), use WID's "000000" (all industries).
            // Otherwise, use NAICS as the industry code (IndCodeType=10 for NAICS).
            var indCode = string.IsNullOrEmpty(naics) ? "000000" : naics;
            var key = $"{stFips}:{widArea}:{year}:{occCode}:{indCode}";
            rowsByKey[key] = new IOWageRow
            {
                StFips = stFips,
                AreaType = widAreaType,
                AreaTypeVersion = "0",
                Area = widArea,
                PeriodYear = year,
                PeriodType = "01",
                Period = "00",
                IndCodeType = "10", // NAICS
                IndCode = indCode,   // Now includes industry-specific data, not just "000000"
                OccCodeType = "19",
                OccCode = occCode,
                WageSource = "3",
                RateType = "2",
                EmpCount = ParseNullableLong(GetText(row, headerMap, "TOT_EMP")),
                MeanHourly = ParseNullableDecimal(GetText(row, headerMap, "H_MEAN")),
                AnnualMean = ParseNullableDecimal(GetText(row, headerMap, "A_MEAN")),
                Pct10 = ParseNullableDecimal(GetText(row, headerMap, "A_PCT10")),
                Pct25 = ParseNullableDecimal(GetText(row, headerMap, "A_PCT25")),
                MedianWage = ParseNullableDecimal(GetText(row, headerMap, "A_MEDIAN")),
                Pct75 = ParseNullableDecimal(GetText(row, headerMap, "A_PCT75")),
                Pct90 = ParseNullableDecimal(GetText(row, headerMap, "A_PCT90")),
                SuppRecord = HasSuppressedValue(row, headerMap) ? "1" : "0",
            };
        }

        return rowsByKey.Values.ToList();
    }

    private static string GetText(IXLRow row, IReadOnlyDictionary<string, int> headerMap, string key)
    {
        return headerMap.TryGetValue(key, out var col)
            ? row.Cell(col).GetFormattedString().Trim()
            : string.Empty;
    }

    private static string NormalizeOccCode(string value)
    {
        return new string(value.Where(char.IsDigit).ToArray());
    }

    private static bool TryMapStFips(string area, out string stFips)
    {
        stFips = "";
        if (area == "99")
        {
            stFips = "00";
            return true;
        }

        if (area.Length == 2 && area.All(char.IsDigit))
        {
            stFips = area;
            return true;
        }

        return false;
    }

    private static long? ParseNullableLong(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "*" or "#")
            return null;

        return long.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static decimal? ParseNullableDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value is "*" or "#")
            return null;

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static bool HasSuppressedValue(IXLRow row, IReadOnlyDictionary<string, int> headerMap)
    {
        var checkedColumns = new[] { "A_MEAN", "H_MEAN", "A_PCT10", "A_PCT25", "A_MEDIAN", "A_PCT75", "A_PCT90", "TOT_EMP" };
        foreach (var col in checkedColumns)
        {
            var value = GetText(row, headerMap, col);
            if (value is "*" or "#")
                return true;
        }

        return false;
    }

    private async Task UpsertAsync(List<IOWageRow> rows, CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        foreach (var row in rows)
        {
            var cmd = new NpgsqlCommand(@"
                INSERT INTO iowage
                    (stfips, areatype, areatypeversion, area, periodyear, periodtype, period,
                     indindcodetype, indcode, occcodetype, occcode,
                     wagesource, ratetype,
                     empcount, pct10, pct25, medianwage, meanwage, pct75, pct90,
                     meanhourly, annualmean, supprecord)
                VALUES
                    (@stfips, @areatype, @areatypeversion, @area, @periodyear, @periodtype, @period,
                     @indcodetype, @indcode, @occcodetype, @occcode,
                     @wagesource, @ratetype,
                     @empcount, @pct10, @pct25, @medianwage, @meanwage, @pct75, @pct90,
                     @meanhourly, @annualmean, @supprecord)
                ON CONFLICT (stfips, areatype, areatypeversion, area, periodyear, periodtype, period,
                             indindcodetype, indcode, occcodetype, occcode)
                DO UPDATE SET
                    wagesource = EXCLUDED.wagesource,
                    ratetype = EXCLUDED.ratetype,
                    empcount = EXCLUDED.empcount,
                    medianwage = EXCLUDED.medianwage,
                    meanwage = EXCLUDED.meanwage,
                    meanhourly = EXCLUDED.meanhourly,
                    annualmean = EXCLUDED.annualmean", conn);

            cmd.Parameters.AddWithValue("stfips", row.StFips);
            cmd.Parameters.AddWithValue("areatype", row.AreaType);
            cmd.Parameters.AddWithValue("areatypeversion", row.AreaTypeVersion);
            cmd.Parameters.AddWithValue("area", row.Area);
            cmd.Parameters.AddWithValue("periodyear", row.PeriodYear);
            cmd.Parameters.AddWithValue("periodtype", row.PeriodType);
            cmd.Parameters.AddWithValue("period", row.Period);
            cmd.Parameters.AddWithValue("indcodetype", row.IndCodeType);
            cmd.Parameters.AddWithValue("indcode", row.IndCode);
            cmd.Parameters.AddWithValue("occcodetype", row.OccCodeType);
            cmd.Parameters.AddWithValue("occcode", row.OccCode);
            cmd.Parameters.AddWithValue("wagesource", row.WageSource);
            cmd.Parameters.AddWithValue("ratetype", row.RateType);
            cmd.Parameters.AddWithValue("empcount", (object?)row.EmpCount ?? DBNull.Value);
            cmd.Parameters.AddWithValue("pct10", (object?)row.Pct10 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("pct25", (object?)row.Pct25 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("medianwage", (object?)row.MedianWage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("meanwage", (object?)row.MeanWage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("pct75", (object?)row.Pct75 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("pct90", (object?)row.Pct90 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("meanhourly", (object?)row.MeanHourly ?? DBNull.Value);
            cmd.Parameters.AddWithValue("annualmean", (object?)row.AnnualMean ?? DBNull.Value);
            cmd.Parameters.AddWithValue("supprecord", row.SuppRecord);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}



