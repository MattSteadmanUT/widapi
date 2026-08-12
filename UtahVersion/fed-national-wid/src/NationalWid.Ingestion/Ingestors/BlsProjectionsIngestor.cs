using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using NationalWid.Ingestion.Models;
using NationalWid.Ingestion.Services;
using Npgsql;

namespace NationalWid.Ingestion.Ingestors;

/// <summary>
/// Fetches long-term and short-term occupational projections from ProjectionsCentral REST JSON and upserts
/// into the <c>projectionsmatrix</c> table.
/// Sources: 
///   - https://public.projectionscentral.org/Projections/LongTermRestJson
///   - https://public.projectionscentral.org/Projections/ShortTermRestJson
/// </summary>
public sealed class BlsProjectionsIngestor(
    HttpClient httpClient,
    string connectionString,
    SourceHashService sourceHashService,
    ILogger logger)
{
    private const string LongTermEndpoint = "https://public.projectionscentral.org/Projections/LongTermRestJson";
    private const string ShortTermEndpoint = "https://public.projectionscentral.org/Projections/ShortTermRestJson";

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var allRows = new List<ProjectionsMatrixRow>();
        var occDirectoryRows = new Dictionary<string, (string StFips, string ProjPeriod, string OccCodeType, string OccCode, string OccTitle)>(StringComparer.Ordinal);

        try
        {
            logger.LogInformation("Projections: fetching long-term projections");
            var longTermRows = await FetchAllPagesAsync(LongTermEndpoint, occDirectoryRows, cancellationToken);
            allRows.AddRange(longTermRows);
            logger.LogInformation("Projections: long-term returned {Count} rows", longTermRows.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Projections: error fetching long-term: {Message}", ex.Message);
        }

        try
        {
            logger.LogInformation("Projections: fetching short-term projections");
            var shortTermRows = await FetchAllPagesAsync(ShortTermEndpoint, occDirectoryRows, cancellationToken);
            allRows.AddRange(shortTermRows);
            logger.LogInformation("Projections: short-term returned {Count} rows", shortTermRows.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning("Projections: error fetching short-term: {Message}", ex.Message);
        }

        logger.LogInformation("Projections: total rows from both endpoints: {Total}", allRows.Count);

        if (allRows.Count == 0)
        {
            logger.LogWarning("Projections: no rows returned from ProjectionsCentral");
            await MaterializeMatrixCrosswalksAsync(cancellationToken);
            return 0;
        }

        logger.LogInformation("Projections: upserting {Count} rows", allRows.Count);
        await UpsertAsync(allRows, cancellationToken);

        if (occDirectoryRows.Count > 0)
        {
            logger.LogInformation("Projections: upserting {Count} occupation directory titles", occDirectoryRows.Count);
            await UpsertOccDirectoriesAsync(occDirectoryRows.Values.ToList(), cancellationToken);
        }

        await MaterializeMatrixCrosswalksAsync(cancellationToken);
        return allRows.Count;
    }

    private async Task<List<ProjectionsMatrixRow>> FetchAllPagesAsync(
        string endpoint,
        Dictionary<string, (string StFips, string ProjPeriod, string OccCodeType, string OccCode, string OccTitle)> occDirectoryRows,
        CancellationToken cancellationToken)
    {
        var allRows = new List<ProjectionsMatrixRow>();
        var page = 0;
        var pageSize = 1000; // API max is 1000

        while (true)
        {
            var pageResult = await FetchPageAsync(endpoint, page, pageSize, occDirectoryRows, cancellationToken);
            if (pageResult.Rows.Count > 0)
            {
                allRows.AddRange(pageResult.Rows);
                logger.LogInformation("Projections: {Endpoint} page {Page} returned {Count} rows (total so far: {Total})",
                    endpoint, page, pageResult.Rows.Count, allRows.Count);
            }
            else if (pageResult.TotalPages == 0)
            {
                break;
            }

            // Check if we've fetched all pages
            if (pageResult.TotalPages > 0 && page >= pageResult.TotalPages - 1)
                break;

            page++;
        }

        logger.LogInformation("Projections: {Endpoint} complete with {Total} rows across {Pages} pages",
            endpoint, allRows.Count, page + 1);
        return allRows;
    }

    private async Task<ProjectionsPageResult> FetchPageAsync(
        string endpoint,
        int page,
        int pageSize,
        Dictionary<string, (string StFips, string ProjPeriod, string OccCodeType, string OccCode, string OccTitle)> occDirectoryRows,
        CancellationToken cancellationToken)
    {
        var url = $"{endpoint}?items_per_page={pageSize}&page={page}";

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(15)); // 15 second timeout per page

        try
        {
            using var response = await httpClient.GetAsync(url, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Projections: request failed on page {Page} with {Status}", page, response.StatusCode);
                return new ProjectionsPageResult([], 0);
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cts.Token);
            var hash = SourceHashService.ComputeSha256Hex(bytes);
            var payload = System.Text.Json.JsonSerializer.Deserialize<ProjectionsRestResponse>(bytes);
            var totalPages = payload?.Pager?.TotalPages ?? 0;
            var changed = await sourceHashService.ShouldProcessAsync(
                connectionString,
                endpoint.Contains("LongTerm", StringComparison.OrdinalIgnoreCase) ? "BLS-PROJECTIONS-LONG" : "BLS-PROJECTIONS-SHORT",
                url,
                hash,
                bytes.LongLength,
                cts.Token);

            if (!changed)
            {
                return new ProjectionsPageResult([], totalPages);
            }

            if (payload?.Rows is null || payload.Rows.Count == 0)
                return new ProjectionsPageResult([], totalPages);

            var rowsByKey = new Dictionary<string, ProjectionsMatrixRow>(StringComparer.Ordinal);
            foreach (var source in payload.Rows)
            {
                if (!TryNormalizeStFips(source.STFIPS, out var stFips))
                    continue;

                var occCode = NormalizeOccCode(source.OccCode);
                if (string.IsNullOrEmpty(occCode))
                    continue;

                var baseYear = ParseInt(source.BaseYear);
                var projYear = ParseInt(source.ProjYear);
                if (baseYear is null || projYear is null)
                    continue;

                var projectionsPeriod = $"{baseYear:0000}-{projYear:0000}";
                var areaType = stFips == "00" ? "00" : "01";
                var area = stFips == "00" ? "000000" : stFips.PadLeft(6, '0');
                var suppRecord = string.IsNullOrWhiteSpace(source.Base) || string.IsNullOrWhiteSpace(source.Projected)
                    ? "1"
                    : "0";

                var row = new ProjectionsMatrixRow
                {
                    StFips = stFips,
                    AreaType = areaType,
                    AreaTypeVersion = "0",
                    Area = area,
                    ProjectionsPeriod = projectionsPeriod,
                    IndCodeType = "10",
                    IndCode = "000000",
                    OccCodeType = "19",
                    OccCode = occCode,
                    BaseYearEmp = ParseDecimal(source.Base),
                    ProjectedEmp = ParseDecimal(source.Projected),
                    Change = ParseDecimal(source.Change),
                    PctChange = ParseDecimal(source.PercentChange),
                    Openings = ParseDecimal(source.AvgAnnualOpenings),
                    SuppRecord = suppRecord,
                };

                var key =
                    $"{row.StFips}:{row.AreaType}:{row.AreaTypeVersion}:{row.Area}:{row.ProjectionsPeriod}:{row.IndCodeType}:{row.IndCode}:{row.OccCodeType}:{row.OccCode}";
                rowsByKey[key] = row;

                var occTitle = source.Title?.Trim();
                if (!string.IsNullOrWhiteSpace(occTitle))
                {
                    var occKey = $"{row.StFips}:{row.ProjectionsPeriod}:{row.OccCodeType}:{row.OccCode}";
                    occDirectoryRows[occKey] = (row.StFips, row.ProjectionsPeriod, row.OccCodeType, row.OccCode, occTitle);
                }
            }

            return new ProjectionsPageResult(rowsByKey.Values.ToList(), totalPages);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Projections: timeout fetching page {Page}", page);
            return new ProjectionsPageResult([], 0);
        }
    }

    private async Task UpsertAsync(List<ProjectionsMatrixRow> rows, CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        foreach (var chunk in rows.Chunk(500))
        {
            await using var tx = await conn.BeginTransactionAsync(cancellationToken);
            await using var batch = new NpgsqlBatch(conn, tx);

            foreach (var row in chunk)
            {
                var cmd = new NpgsqlBatchCommand(@"
                    INSERT INTO projectionsmatrix
                        (stfips, areatype, areatypeversion, area, projectionsperiod,
                         indindcodetype, indcode, occcodetype, occcode,
                         baseyearemp, projectedemp, change, pctchange, openings, supprecord)
                    VALUES
                        (@stfips, @areatype, @areatypeversion, @area, @projectionsperiod,
                         @indcodetype, @indcode, @occcodetype, @occcode,
                         @baseyearemp, @projectedemp, @change, @pctchange, @openings, @supprecord)
                    ON CONFLICT (stfips, areatype, areatypeversion, area, projectionsperiod,
                                 indindcodetype, indcode, occcodetype, occcode)
                    DO UPDATE SET
                        baseyearemp = EXCLUDED.baseyearemp,
                        projectedemp = EXCLUDED.projectedemp,
                        change = EXCLUDED.change,
                        pctchange = EXCLUDED.pctchange,
                        openings = EXCLUDED.openings,
                        supprecord = EXCLUDED.SuppRecord");

                cmd.Parameters.AddWithValue("stfips", row.StFips);
                cmd.Parameters.AddWithValue("areatype", row.AreaType);
                cmd.Parameters.AddWithValue("areatypeversion", row.AreaTypeVersion);
                cmd.Parameters.AddWithValue("area", row.Area);
                cmd.Parameters.AddWithValue("projectionsperiod", row.ProjectionsPeriod);
                cmd.Parameters.AddWithValue("indcodetype", row.IndCodeType);
                cmd.Parameters.AddWithValue("indcode", row.IndCode);
                cmd.Parameters.AddWithValue("occcodetype", row.OccCodeType);
                cmd.Parameters.AddWithValue("occcode", row.OccCode);
                cmd.Parameters.AddWithValue("baseyearemp", (object?)row.BaseYearEmp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("projectedemp", (object?)row.ProjectedEmp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("change", (object?)row.Change ?? DBNull.Value);
                cmd.Parameters.AddWithValue("pctchange", (object?)row.PctChange ?? DBNull.Value);
                cmd.Parameters.AddWithValue("openings", (object?)row.Openings ?? DBNull.Value);
                cmd.Parameters.AddWithValue("supprecord", row.SuppRecord);

                batch.BatchCommands.Add(cmd);
            }

            await batch.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
    }

    private async Task UpsertOccDirectoriesAsync(
        List<(string StFips, string ProjPeriod, string OccCodeType, string OccCode, string OccTitle)> rows,
        CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        foreach (var chunk in rows.Chunk(500))
        {
            await using var tx = await conn.BeginTransactionAsync(cancellationToken);
            await using var batch = new NpgsqlBatch(conn, tx);

            foreach (var row in chunk)
            {
                var cmd = new NpgsqlBatchCommand(@"
                    INSERT INTO occdirectories (stfips, projperiod, occcodetype, occcode, occtitle)
                    VALUES (@stfips, @projperiod, @occcodetype, @occcode, @occtitle)
                    ON CONFLICT (stfips, projperiod, occcodetype, occcode)
                    DO UPDATE SET
                        occtitle = EXCLUDED.occtitle");

                cmd.Parameters.AddWithValue("stfips", row.StFips);
                cmd.Parameters.AddWithValue("projperiod", row.ProjPeriod);
                cmd.Parameters.AddWithValue("occcodetype", row.OccCodeType);
                cmd.Parameters.AddWithValue("occcode", row.OccCode);
                cmd.Parameters.AddWithValue("occtitle", row.OccTitle);
                batch.BatchCommands.Add(cmd);
            }

            await batch.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
    }

    private async Task MaterializeMatrixCrosswalksAsync(CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);

        var replaceIndustryCrosswalks = new NpgsqlCommand(@"
            TRUNCATE TABLE matrixxind;

            INSERT INTO matrixxind (stfips, matrixindcode, indcodetype, indcode)
            SELECT DISTINCT
                stfips,
                indcode AS matrixindcode,
                indcodetype,
                indcode
            FROM inddirectories
            WHERE COALESCE(NULLIF(btrim(indcode), ''), '') <> '';
        ", conn, tx);

        var replaceOccupationCrosswalks = new NpgsqlCommand(@"
            TRUNCATE TABLE matrixxocc;

            INSERT INTO matrixxocc (stfips, matrixocccode, occcodetype, occcode)
            SELECT DISTINCT
                stfips,
                occcode AS matrixocccode,
                occcodetype,
                occcode
            FROM occdirectories
            WHERE COALESCE(NULLIF(btrim(occcode), ''), '') <> '';
        ", conn, tx);

        await replaceIndustryCrosswalks.ExecuteNonQueryAsync(cancellationToken);
        await replaceOccupationCrosswalks.ExecuteNonQueryAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        logger.LogInformation("Projections: refreshed MatrixXInd and MatrixXOcc from directory tables");
    }

    private static bool TryNormalizeStFips(string? value, out string stFips)
    {
        stFips = "";
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return false;

        stFips = parsed.ToString("00", CultureInfo.InvariantCulture);
        return true;
    }

    private static string NormalizeOccCode(string? occCode)
    {
        if (string.IsNullOrWhiteSpace(occCode))
            return "";

        return new string(occCode.Where(char.IsDigit).ToArray());
    }

    private static int? ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }
}

internal sealed record ProjectionsPageResult(List<ProjectionsMatrixRow> Rows, int TotalPages);

internal sealed class ProjectionsRestResponse
{
    [JsonPropertyName("rows")]
    public List<ProjectionsRestRow> Rows { get; set; } = [];

    [JsonPropertyName("pager")]
    public ProjectionsPager? Pager { get; set; }
}

internal sealed class ProjectionsPager
{
    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }
}

internal sealed class ProjectionsRestRow
{
    public string? Area { get; set; }
    public string? Title { get; set; }
    public string? Base { get; set; }
    public string? Projected { get; set; }
    public string? Change { get; set; }
    public string? PercentChange { get; set; }
    public string? AvgAnnualOpenings { get; set; }
    public string? STFIPS { get; set; }
    public string? StateURL { get; set; }
    public string? OccCode { get; set; }
    public string? BaseYear { get; set; }
    public string? ProjYear { get; set; }
}


