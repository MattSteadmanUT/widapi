using System.Globalization;
using Microsoft.Extensions.Logging;
using Npgsql;
using NationalWid.Ingestion.Services;

namespace NationalWid.Ingestion.Ingestors;

/// <summary>
/// Ingests Bureau of Labor Statistics Consumer Price Index (CPI) data from the BLS
/// public flat-file archive at <c>download.bls.gov/pub/time.series/cu/</c>.
///
/// The CPI (All Urban Consumers, CPI-U) series tracks price changes for a market basket
/// of goods and services purchased by urban consumers — an essential complement to wage
/// and employment data for understanding real purchasing power.
///
/// Files fetched:
///   <list type="bullet">
///     <item><c>cu.item</c>     — item code reference (price basket components)</item>
///     <item><c>cu.area</c>     — geographic area codes</item>
///     <item><c>cu.series</c>   — series definitions (area × item × seasonality)</item>
///     <item><c>cu.data.0.Current</c> — recent observation values (rolling window)</item>
///     <item><c>cu.data.1.AllItems</c> — historical All-items observations</item>
///   </list>
///
/// Observation data is upserted into the <c>cpi</c> table using PostgreSQL
/// <c>INSERT … ON CONFLICT DO UPDATE</c> so re-runs are idempotent.
/// Reference data (items, areas, series) is fully replaced on each run.
/// </summary>
public sealed class BlsCpiIngestor(
    HttpClient httpClient,
    string connectionString,
    SourceHashService sourceHashService,
    ILogger logger)
{
    private static readonly string BaseUrl = ResolveBaseUrl(
        Environment.GetEnvironmentVariable("BLS_FLATFILE_BASE_URL"));

    // Data files to ingest — ordered so the broader AllItems file can supplement Current.
    private static readonly string[] DataFiles =
    [
        "cu.data.1.AllItems",   // All-items series, full history
        "cu.data.0.Current",    // Most-recent values for all series
    ];

    // -------------------------------------------------------------------------
    // Public entry point
    // -------------------------------------------------------------------------

    /// <summary>
    /// Downloads and upserts all CPI reference and observation data.
    /// Returns the total number of observation rows upserted.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        await UpsertItemsAsync(cancellationToken);
        await UpsertAreasAsync(cancellationToken);
        await UpsertPeriodicitiesAsync(cancellationToken);
        await UpsertSeasonalAdjustmentsAsync(cancellationToken);
        await UpsertSeriesAsync(cancellationToken);

        var totalRows = 0;
        foreach (var dataFile in DataFiles)
        {
            totalRows += await UpsertDataFileAsync(dataFile, cancellationToken);
        }

        logger.LogInformation("CPI: ingestion complete — {TotalRows} observations upserted", totalRows);
        return totalRows;
    }

    // -------------------------------------------------------------------------
    // Reference-table loaders
    // -------------------------------------------------------------------------

    private async Task UpsertItemsAsync(CancellationToken cancellationToken)
    {
        var rows = await FetchTsvRowsAsync("cu.item", cancellationToken);

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        foreach (var row in rows)
        {
            if (row.Length < 2) continue;
            var itemCode = row[0].Trim();
            var itemName = row[1].Trim();
            if (string.IsNullOrWhiteSpace(itemCode)) continue;

            var cmd = new NpgsqlCommand(@"
                INSERT INTO cpitems (itemcode, itemname)
                VALUES (@code, @name)
                ON CONFLICT (itemcode) DO UPDATE SET itemname = EXCLUDED.itemname;", conn);
            cmd.Parameters.AddWithValue("code", itemCode);
            cmd.Parameters.AddWithValue("name", itemName);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        logger.LogInformation("CPI: upserted {Count} item codes", rows.Count);
    }

    private async Task UpsertAreasAsync(CancellationToken cancellationToken)
    {
        var rows = await FetchTsvRowsAsync("cu.area", cancellationToken);

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        foreach (var row in rows)
        {
            if (row.Length < 2) continue;
            var areaCode = row[0].Trim();
            var areaName = row[1].Trim();
            if (string.IsNullOrWhiteSpace(areaCode)) continue;

            var cmd = new NpgsqlCommand(@"
                INSERT INTO cpiareas (areacode, areaname)
                VALUES (@code, @name)
                ON CONFLICT (areacode) DO UPDATE SET areaname = EXCLUDED.areaname;", conn);
            cmd.Parameters.AddWithValue("code", areaCode);
            cmd.Parameters.AddWithValue("name", areaName);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        logger.LogInformation("CPI: upserted {Count} area codes", rows.Count);
    }

    private async Task UpsertSeriesAsync(CancellationToken cancellationToken)
    {
        var rows = await FetchTsvRowsAsync("cu.series", cancellationToken);

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        var upserted = 0;
        foreach (var row in rows)
        {
            // BLS cu.series columns (0-indexed):
            // 0=series_id, 1=area_code, 2=item_code, 3=seasonal, 4=periodicity_code,
            // 5=base_code, 6=base_period, 7=series_title, 8=footnote_codes,
            // 9=begin_year, 10=begin_period, 11=end_year, 12=end_period
            if (row.Length < 13) continue;

            var seriesId = row[0].Trim();
            if (string.IsNullOrWhiteSpace(seriesId)) continue;

            short.TryParse(row[9].Trim(), out var beginYear);
            short.TryParse(row[11].Trim(), out var endYear);

            var cmd = new NpgsqlCommand(@"
                INSERT INTO cpiseries
                    (seriesid, seasonalcode, periodicitycode, areacode, itemcode,
                     basetypecode, baseyear, footnotecodesstr,
                     beginperiod, beginyear, endperiod, endyear, seriesname)
                VALUES
                    (@seriesId, @seasonal, @periodicity, @area, @item,
                     @baseType, @baseYear, @footnotes,
                     @beginPeriod, @beginYear, @endPeriod, @endYear, @seriesName)
                ON CONFLICT (seriesid) DO UPDATE SET
                    seasonalcode     = EXCLUDED.seasonalcode,
                    periodicitycode  = EXCLUDED.periodicitycode,
                    areacode         = EXCLUDED.areacode,
                    itemcode         = EXCLUDED.itemcode,
                    basetypecode     = EXCLUDED.basetypecode,
                    baseyear         = EXCLUDED.baseyear,
                    footnotecodesstr = EXCLUDED.footnotecodesstr,
                    beginperiod      = EXCLUDED.beginperiod,
                    beginyear        = EXCLUDED.beginyear,
                    endperiod        = EXCLUDED.endperiod,
                    endyear          = EXCLUDED.endyear,
                    seriesname       = EXCLUDED.seriesname;", conn);

            cmd.Parameters.AddWithValue("seriesId",    seriesId);
            cmd.Parameters.AddWithValue("seasonal",    row[3].Trim());
            cmd.Parameters.AddWithValue("periodicity", row[4].Trim());
            cmd.Parameters.AddWithValue("area",        row[1].Trim());
            cmd.Parameters.AddWithValue("item",        row[2].Trim());
            cmd.Parameters.AddWithValue("baseType",    NpgsqlNullable(row.Length > 5 ? row[5].Trim() : null));
            cmd.Parameters.AddWithValue("baseYear",    NpgsqlNullable(row.Length > 6 ? row[6].Trim() : null));
            cmd.Parameters.AddWithValue("footnotes",   NpgsqlNullable(row.Length > 8 ? row[8].Trim() : null));
            cmd.Parameters.AddWithValue("beginPeriod", NpgsqlNullable(row.Length > 10 ? row[10].Trim() : null));
            cmd.Parameters.AddWithValue("beginYear",   beginYear > 0 ? beginYear : DBNull.Value);
            cmd.Parameters.AddWithValue("endPeriod",   NpgsqlNullable(row.Length > 12 ? row[12].Trim() : null));
            cmd.Parameters.AddWithValue("endYear",     endYear > 0 ? endYear : DBNull.Value);
            cmd.Parameters.AddWithValue("seriesName",  NpgsqlNullable(row.Length > 7 ? row[7].Trim() : null));

            await cmd.ExecuteNonQueryAsync(cancellationToken);
            upserted++;
        }

        logger.LogInformation("CPI: upserted {Count} series", upserted);
    }

    private async Task UpsertPeriodicitiesAsync(CancellationToken cancellationToken)
    {
        var rows = await FetchTsvRowsAsync("cu.periodicity", cancellationToken);

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        foreach (var row in rows)
        {
            if (row.Length < 2) continue;
            var code = row[0].Trim();
            var name = row[1].Trim();
            if (string.IsNullOrWhiteSpace(code)) continue;

            var cmd = new NpgsqlCommand(@"
                INSERT INTO cpiperiodicities (periodicitycode, periodicityname)
                VALUES (@code, @name)
                ON CONFLICT (periodicitycode) DO UPDATE SET periodicityname = EXCLUDED.periodicityname;", conn);
            cmd.Parameters.AddWithValue("code", code);
            cmd.Parameters.AddWithValue("name", name);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        logger.LogInformation("CPI: upserted {Count} periodicity codes", rows.Count);
    }

    private async Task UpsertSeasonalAdjustmentsAsync(CancellationToken cancellationToken)
    {
        var rows = await FetchTsvRowsAsync("cu.seasonal", cancellationToken);

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        foreach (var row in rows)
        {
            if (row.Length < 2) continue;
            var code = row[0].Trim();
            var name = row[1].Trim();
            if (string.IsNullOrWhiteSpace(code)) continue;

            var cmd = new NpgsqlCommand(@"
                INSERT INTO cpiseasonaladjustments (seasonalcode, seasonalname)
                VALUES (@code, @name)
                ON CONFLICT (seasonalcode) DO UPDATE SET seasonalname = EXCLUDED.seasonalname;", conn);
            cmd.Parameters.AddWithValue("code", code);
            cmd.Parameters.AddWithValue("name", name);
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        logger.LogInformation("CPI: upserted {Count} seasonal adjustment codes", rows.Count);
    }

    // -------------------------------------------------------------------------
    // Observation data loader
    // -------------------------------------------------------------------------

    private async Task<int> UpsertDataFileAsync(string dataFile, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/{dataFile}";
        var tempFile = Path.GetTempFileName();
        int rowsUpserted = 0;

        try
        {
            await DownloadToFileAsync(url, tempFile, cancellationToken);
            if (!File.Exists(tempFile) || new FileInfo(tempFile).Length == 0)
            {
                logger.LogWarning("CPI: data file {File} was empty or not downloaded", dataFile);
                return 0;
            }

            // Check whether source has changed since last run.
            var hash = await SourceHashService.ComputeFileSha256HexAsync(tempFile, cancellationToken);
            var datasetKey = $"BLS-FLAT-CPI-{dataFile.ToUpperInvariant()}";
            var shouldProcess = await sourceHashService.ShouldProcessAsync(
                connectionString, datasetKey, url, hash, new FileInfo(tempFile).Length, cancellationToken);

            if (!shouldProcess)
            {
                logger.LogInformation("CPI: {File} hash unchanged — skipping", dataFile);
                return 0;
            }

            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);

            using var reader = new StreamReader(tempFile);
            var lineNumber = 0;

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Skip header row
                if (lineNumber == 1 && line.Contains("series_id", StringComparison.OrdinalIgnoreCase))
                    continue;

                var cols = line.Split('\t', StringSplitOptions.TrimEntries);
                if (cols.Length < 4) continue;

                var seriesId = cols[0];
                if (!short.TryParse(cols[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var year))
                    continue;

                var period = cols[2];
                decimal? value = null;
                if (decimal.TryParse(cols[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedValue))
                    value = parsedValue;

                var footnotes = cols.Length > 4 ? cols[4] : null;

                var cmd = new NpgsqlCommand(@"
                    INSERT INTO cpi (seriesid, year, period, value, footnotes)
                    VALUES (@seriesId, @year, @period, @value, @footnotes)
                    ON CONFLICT (seriesid, year, period) DO UPDATE SET
                        value     = EXCLUDED.value,
                        footnotes = EXCLUDED.footnotes;", conn);

                cmd.Parameters.AddWithValue("seriesId",  seriesId);
                cmd.Parameters.AddWithValue("year",      year);
                cmd.Parameters.AddWithValue("period",    period);
                cmd.Parameters.AddWithValue("value",     (object?)value ?? DBNull.Value);
                cmd.Parameters.AddWithValue("footnotes", (object?)footnotes ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync(cancellationToken);
                rowsUpserted++;

                if (rowsUpserted % 50_000 == 0)
                    logger.LogInformation("CPI: {File} — {Rows} rows upserted so far", dataFile, rowsUpserted);
            }

            logger.LogInformation("CPI: {File} — {Rows} total rows upserted", dataFile, rowsUpserted);
        }
        finally
        {
            try { File.Delete(tempFile); } catch { /* non-critical */ }
        }

        return rowsUpserted;
    }

    // -------------------------------------------------------------------------
    // HTTP helpers
    // -------------------------------------------------------------------------

    private async Task<List<string[]>> FetchTsvRowsAsync(string fileName, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/{fileName}";
        var tempFile = Path.GetTempFileName();
        var rows = new List<string[]>();

        try
        {
            await DownloadToFileAsync(url, tempFile, cancellationToken);

            using var reader = new StreamReader(tempFile);
            var lineNumber = 0;
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;
                // Skip the header row (BLS flat files always have a tab-separated header).
                if (lineNumber == 1) continue;
                rows.Add(line.Split('\t', StringSplitOptions.TrimEntries));
            }
        }
        finally
        {
            try { File.Delete(tempFile); } catch { /* non-critical */ }
        }

        return rows;
    }

    private async Task DownloadToFileAsync(string url, string destPath, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", "ULMITA/1.0 (ulmita.org; mattsteadman@utah.gov)");
        request.Headers.TryAddWithoutValidation("Accept", "*/*");
        request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "CPI: failed to download {Url}", url);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("CPI: {Url} returned {StatusCode}", url, response.StatusCode);
            return;
        }

        await using var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var destStream = File.Create(destPath);
        await sourceStream.CopyToAsync(destStream, cancellationToken);
    }

    private static object NpgsqlNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;

    internal static string ResolveBaseUrl(string? configuredBaseUrl)
    {
        var baseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl)
            ? "https://download.bls.gov/pub/time.series"
            : configuredBaseUrl.Trim().TrimEnd('/');

        return baseUrl.EndsWith("/cu", StringComparison.OrdinalIgnoreCase)
            ? baseUrl
            : $"{baseUrl}/cu";
    }
}
