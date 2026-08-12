using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NationalWid.Api.Data;
using NationalWid.Api.Models;
using NationalWid.Api.Services;

namespace NationalWid.Api.Controllers;

/// <summary>
/// Provides access to Bureau of Labor Statistics Consumer Price Index (CPI) data.
///
/// The CPI measures the average change over time in the prices paid by urban consumers
/// for a market basket of goods and services.  Data is sourced from the BLS Time Series
/// public flat files at <c>download.bls.gov/pub/time.series/cu/</c>.
///
/// All endpoints support pagination (<c>page</c>/<c>pageSize</c> or <c>cursor</c>),
/// sorting (<c>sort=field:asc</c>), and most filters accept multi-value CSV and wildcard
/// patterns (e.g., <c>itemCode=SA0,SAF1</c> or <c>seriesId=CUSR*</c>).
/// </summary>
[ApiController]
[Route("cpi")]
[Authorize]
public sealed class CpiController(
    WIDDbContext db,
    IQueryService queryService,
    IDownloadService downloadService) : ControllerBase
{
    // -------------------------------------------------------------------------
    // GET /cpi  —  CPI observation values
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns CPI observation values for the specified series, year, and period filters.
    /// Each row is one index value for a series/year/period combination.
    /// </summary>
    /// <param name="seriesId">Filter by series ID or pattern (supports wildcard <c>*</c>, CSV multi-value).</param>
    /// <param name="year">Filter by year (4-digit, supports CSV multi-value).</param>
    /// <param name="period">Filter by BLS period code (e.g., <c>M01</c>, <c>M13</c>, supports CSV).</param>
    /// <param name="areaCode">Filter by BLS area code (resolved via series join).</param>
    /// <param name="itemCode">Filter by BLS item code (e.g., <c>SA0</c>, supports CSV).</param>
    /// <param name="seasonalCode">Filter by seasonal adjustment code (<c>S</c> or <c>U</c>).</param>
    /// <param name="sort">Sort expression, e.g., <c>sort=seriesId:asc,year:desc</c>.</param>
    /// <param name="format">Download format: <c>csv</c>, <c>tsv</c>, <c>psv</c>, <c>xlsx</c>, <c>json</c>.</param>
    /// <param name="page">1-based page number for offset pagination.</param>
    /// <param name="pageSize">Records per page (default 100, max varies by format).</param>
    /// <param name="cursor">Opaque cursor token for cursor-based pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [AllowAnonymous]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> Get(
        [FromQuery] string? seriesId,
        [FromQuery] string? year,
        [FromQuery] string? period,
        [FromQuery] string? areaCode,
        [FromQuery] string? itemCode,
        [FromQuery] string? seasonalCode,
        [FromQuery] string? sort,
        [FromQuery] string? format,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        IQueryable<Cpi> query = db.Cpi.AsNoTracking();

        query = QueryFiltering.ApplyStringFilter(query, nameof(Cpi.SeriesId), seriesId, allowWildcard: true);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Cpi.Year), year);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Cpi.Period), period);

        // Area/item/seasonal filters require a join to the series table.
        if (!string.IsNullOrWhiteSpace(areaCode) || !string.IsNullOrWhiteSpace(itemCode) || !string.IsNullOrWhiteSpace(seasonalCode))
        {
            var seriesQuery = db.CpiSeries.AsNoTracking();
            seriesQuery = QueryFiltering.ApplyStringFilter(seriesQuery, nameof(CpiSeries.AreaCode), areaCode);
            seriesQuery = QueryFiltering.ApplyStringFilter(seriesQuery, nameof(CpiSeries.ItemCode), itemCode);
            seriesQuery = QueryFiltering.ApplyStringFilter(seriesQuery, nameof(CpiSeries.SeasonalCode), seasonalCode);
            var matchingSeriesIds = seriesQuery.Select(s => s.SeriesId);
            query = query.Where(c => matchingSeriesIds.Contains(c.SeriesId));
        }

        var ordered = QuerySorting.Apply(query, sort, nameof(Cpi.SeriesId), nameof(Cpi.Year), nameof(Cpi.Period));

        var resolvedFormat = downloadService.ResolveFormat(Request, format);
        if (resolvedFormat != DownloadFormat.Json)
        {
            var exportLimit = 100_000;
            var rows = await ordered.Take(exportLimit).ToListAsync(cancellationToken);
            return resolvedFormat switch
            {
                DownloadFormat.Tsv      => downloadService.TsvResult(rows, "cpi"),
                DownloadFormat.Psv      => downloadService.PsvResult(rows, "cpi"),
                DownloadFormat.JsonFile => downloadService.JsonFileResult(rows, "cpi"),
                DownloadFormat.Xlsx     => downloadService.XlsxResult(rows, "cpi"),
                _                       => downloadService.CsvResult(rows, "cpi"),
            };
        }

        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    // -------------------------------------------------------------------------
    // GET /cpi/metadata  —  distinct series/area/item combinations present in the DB
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns distinct series metadata present in the database, joined with area and item lookups.
    /// Useful for discovery — shows which series IDs, areas, and item baskets are available.
    /// </summary>
    /// <param name="areaCode">Filter by area code.</param>
    /// <param name="itemCode">Filter by item code.</param>
    /// <param name="seasonalCode">Filter by seasonal adjustment code.</param>
    /// <param name="sort">Sort expression.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Records per page.</param>
    /// <param name="cursor">Opaque cursor token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("metadata")]
    [AllowAnonymous]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> GetMetadata(
        [FromQuery] string? areaCode,
        [FromQuery] string? itemCode,
        [FromQuery] string? seasonalCode,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        IQueryable<CpiSeries> query = db.CpiSeries.AsNoTracking();

        query = QueryFiltering.ApplyStringFilter(query, nameof(CpiSeries.AreaCode), areaCode);
        query = QueryFiltering.ApplyStringFilter(query, nameof(CpiSeries.ItemCode), itemCode);
        query = QueryFiltering.ApplyStringFilter(query, nameof(CpiSeries.SeasonalCode), seasonalCode);

        // Join with area/item names for human-readable response.
        var enriched =
            from s in query
            join a in db.CpiAreas.AsNoTracking() on s.AreaCode equals a.AreaCode into aJoin
            from a in aJoin.DefaultIfEmpty()
            join i in db.CpiItems.AsNoTracking() on s.ItemCode equals i.ItemCode into iJoin
            from i in iJoin.DefaultIfEmpty()
            select new
            {
                s.SeriesId,
                s.SeasonalCode,
                s.PeriodicityCode,
                s.AreaCode,
                AreaName = a != null ? a.AreaName : null,
                s.ItemCode,
                ItemName = i != null ? i.ItemName : null,
                s.BaseYear,
                s.BeginYear,
                s.BeginPeriod,
                s.EndYear,
                s.EndPeriod,
                s.SeriesName,
            };

        var ordered = QuerySorting.Apply(enriched, sort, "AreaCode", "ItemCode", "SeasonalCode", "SeriesId");
        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    // -------------------------------------------------------------------------
    // GET /cpi/items  —  item code reference table
    // -------------------------------------------------------------------------

    /// <summary>Returns all CPI item (price basket component) codes and their descriptions.</summary>
    /// <param name="itemCode">Filter by item code or pattern.</param>
    /// <param name="sort">Sort expression.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Records per page.</param>
    /// <param name="cursor">Opaque cursor token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("items")]
    [AllowAnonymous]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> GetItems(
        [FromQuery] string? itemCode,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        IQueryable<CpiItem> query = db.CpiItems.AsNoTracking();
        query = QueryFiltering.ApplyStringFilter(query, nameof(CpiItem.ItemCode), itemCode, allowWildcard: true);
        var ordered = QuerySorting.Apply(query, sort, nameof(CpiItem.ItemCode));
        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    // -------------------------------------------------------------------------
    // GET /cpi/areas  —  geographic area reference table
    // -------------------------------------------------------------------------

    /// <summary>Returns all CPI geographic area codes and their names.</summary>
    /// <param name="areaCode">Filter by area code.</param>
    /// <param name="sort">Sort expression.</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Records per page.</param>
    /// <param name="cursor">Opaque cursor token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("areas")]
    [AllowAnonymous]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> GetAreas(
        [FromQuery] string? areaCode,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        IQueryable<CpiArea> query = db.CpiAreas.AsNoTracking();
        query = QueryFiltering.ApplyStringFilter(query, nameof(CpiArea.AreaCode), areaCode);
        var ordered = QuerySorting.Apply(query, sort, nameof(CpiArea.AreaCode));
        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }
}
