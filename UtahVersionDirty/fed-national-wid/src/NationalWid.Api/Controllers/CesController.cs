using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NationalWid.Api.Data;
using NationalWid.Api.Models;
using NationalWid.Api.Services;

namespace NationalWid.Api.Controllers;

/// <summary>WID 3.0 CES table (Current Employment Statistics).</summary>
[ApiController]
[Authorize]
[Route("ces")]
public sealed class CesController(
    WIDDbContext db,
    IQueryService queryService,
    IDownloadService downloadService,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Returns a paged list of CES records matching the supplied filter parameters.
    /// Supports CSV multi-value filters, wildcard series codes, min/max year range,
    /// user-defined sort, and download in CSV / TSV / PSV / XLSX / JSON formats.
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> Get(
        [FromQuery] string? stFips,
        [FromQuery] string? areaType,
        [FromQuery] string? areaTypeVersion,
        [FromQuery] string? area,
        [FromQuery] string? periodYear,
        [FromQuery] string? periodType,
        [FromQuery] string? period,
        [FromQuery] string? minYear,
        [FromQuery] string? maxYear,
        [FromQuery] string? adjusted,
        [FromQuery] string? seriesCodeType,
        [FromQuery] string? seriesCode,
        [FromQuery] string? sort,
        [FromQuery] string? format,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        IQueryable<Ces> query = db.Ces.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));
        query = QueryFiltering.ApplyStringFilter(query, nameof(Ces.AreaType), areaType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Ces.AreaTypeVersion), areaTypeVersion);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Ces.Area), area, WidCodes.NormalizeArea);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Ces.PeriodYear), periodYear);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Ces.PeriodType), periodType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Ces.Period), period);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Ces.Adjusted), adjusted);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Ces.SeriesCodeType), seriesCodeType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Ces.SeriesCode), seriesCode, WidCodes.NormalizeCodePattern, allowWildcard: true);

        if (!string.IsNullOrWhiteSpace(minYear))
        {
            query = query.Where(x => string.CompareOrdinal(x.PeriodYear, minYear) >= 0);
        }
        if (!string.IsNullOrWhiteSpace(maxYear))
        {
            query = query.Where(x => string.CompareOrdinal(x.PeriodYear, maxYear) <= 0);
        }

        var ordered = QuerySorting.Apply(
            query,
            sort,
            nameof(Ces.StFips), nameof(Ces.AreaType), nameof(Ces.AreaTypeVersion), nameof(Ces.Area),
            nameof(Ces.PeriodYear), nameof(Ces.PeriodType), nameof(Ces.Period),
            nameof(Ces.SeriesCodeType), nameof(Ces.SeriesCode), nameof(Ces.Adjusted));

        var resolvedFormat = downloadService.ResolveFormat(Request, format);
        if (resolvedFormat != DownloadFormat.Json)
        {
            var exportLimit = configuration.GetValue("Api:ExportRowLimit", 100_000);
            var rows = await ordered.Take(exportLimit).ToListAsync(cancellationToken);
            return resolvedFormat switch
            {
                DownloadFormat.Tsv      => downloadService.TsvResult(rows, "ces"),
                DownloadFormat.Psv      => downloadService.PsvResult(rows, "ces"),
                DownloadFormat.JsonFile => downloadService.JsonFileResult(rows, "ces"),
                DownloadFormat.Xlsx     => downloadService.XlsxResult(rows, "ces"),
                _                       => downloadService.CsvResult(rows, "ces"),
            };
        }

        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>
    /// Returns coverage metadata for the CES table under the supplied filters.
    /// Pass <c>metadataFields</c> as a comma-separated list of <c>areas</c>, <c>years</c>,
    /// <c>periods</c>, <c>minPeriod</c>, and/or <c>maxPeriod</c> to select which summaries to compute.
    /// </summary>
    [HttpGet("metadata")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> GetMetadata(
        [FromQuery] string? stFips,
        [FromQuery] string? areaType,
        [FromQuery] string? areaTypeVersion,
        [FromQuery] string? area,
        [FromQuery] string? periodYear,
        [FromQuery] string? periodType,
        [FromQuery] string? period,
        [FromQuery] string? minYear,
        [FromQuery] string? maxYear,
        [FromQuery] string? adjusted,
        [FromQuery] string? seriesCodeType,
        [FromQuery] string? seriesCode,
        [FromQuery] string? metadataFields,
        CancellationToken cancellationToken)
    {
        IQueryable<Ces> query = db.Ces.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));

        query = QueryFiltering.ApplyStringFilter(query, nameof(Ces.AreaType), areaType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Ces.AreaTypeVersion), areaTypeVersion);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Ces.Area), area, WidCodes.NormalizeArea);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Ces.PeriodYear), periodYear);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Ces.PeriodType), periodType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Ces.Period), period);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Ces.Adjusted), adjusted);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Ces.SeriesCodeType), seriesCodeType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Ces.SeriesCode), seriesCode, WidCodes.NormalizeCodePattern, allowWildcard: true);

        if (!string.IsNullOrWhiteSpace(minYear))
        {
            query = query.Where(x => string.CompareOrdinal(x.PeriodYear, minYear) >= 0);
        }
        if (!string.IsNullOrWhiteSpace(maxYear))
        {
            query = query.Where(x => string.CompareOrdinal(x.PeriodYear, maxYear) <= 0);
        }

        var fields = MetadataFields.Parse(metadataFields);
        var metadata = new TableMetadata();

        if (fields.Contains("areas"))
        {
            metadata = metadata with
            {
                Areas = await query
                    .Select(x => new AreaKey
                    {
                        StFips = x.StFips,
                        AreaType = x.AreaType,
                        AreaTypeVersion = x.AreaTypeVersion,
                        Area = x.Area,
                    })
                    .Distinct()
                    .OrderBy(x => x.StFips)
                    .ThenBy(x => x.AreaType)
                    .ThenBy(x => x.AreaTypeVersion)
                    .ThenBy(x => x.Area)
                    .ToListAsync(cancellationToken)
            };
        }

        if (fields.Contains("years"))
        {
            metadata = metadata with
            {
                Years = await query
                    .Select(x => x.PeriodYear)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToListAsync(cancellationToken)
            };
        }

        if (fields.Contains("periods"))
        {
            metadata = metadata with
            {
                Periods = await query
                    .Select(x => new PeriodKey { PeriodType = x.PeriodType, Period = x.Period })
                    .Distinct()
                    .OrderBy(x => x.PeriodType)
                    .ThenBy(x => x.Period)
                    .ToListAsync(cancellationToken)
            };
        }

        if (fields.Contains("minPeriod") || fields.Contains("maxPeriod"))
        {
            var ordered = query
                .OrderBy(x => x.PeriodYear)
                .ThenBy(x => x.PeriodType)
                .ThenBy(x => x.Period);

            var min = fields.Contains("minPeriod")
                ? await ordered.Select(x => new PeriodPoint { PeriodYear = x.PeriodYear, PeriodType = x.PeriodType, Period = x.Period }).FirstOrDefaultAsync(cancellationToken)
                : null;

            var max = fields.Contains("maxPeriod")
                ? await ordered.Select(x => new PeriodPoint { PeriodYear = x.PeriodYear, PeriodType = x.PeriodType, Period = x.Period }).LastOrDefaultAsync(cancellationToken)
                : null;

            metadata = metadata with { MinPeriod = min, MaxPeriod = max };
        }

        return Ok(ResponseEnvelope.CreateObject(metadata, Request));
    }
}
