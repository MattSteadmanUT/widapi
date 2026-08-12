using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NationalWid.Api.Data;
using NationalWid.Api.Models;
using NationalWid.Api.Services;

namespace NationalWid.Api.Controllers;

/// <summary>WID 3.0 Industry table (QCEW / ES-202).</summary>
[ApiController]
[Authorize]
[Route("industry")]
public sealed class IndustryController(
    WIDDbContext db,
    IQueryService queryService,
    IDownloadService downloadService,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Returns a paged list of Industry (QCEW) records matching the supplied filters.
    /// Supports CSV multi-value filters, wildcard industry codes, min/max year range,
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
        [FromQuery] string? ownership,
        [FromQuery] string? codeType,
        [FromQuery] string? indCode,
        [FromQuery] string? sort,
        [FromQuery] string? format,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        IQueryable<Industry> query = db.Industry.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));
        query = QueryFiltering.ApplyStringFilter(query, nameof(Industry.AreaType), areaType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Industry.AreaTypeVersion), areaTypeVersion);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Industry.Area), area, WidCodes.NormalizeArea);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Industry.PeriodYear), periodYear);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Industry.PeriodType), periodType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Industry.Period), period);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Industry.Ownership), ownership);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Industry.IndCodeType), codeType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Industry.IndCode), indCode, WidCodes.NormalizeCodePattern, allowWildcard: true);

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
            nameof(Industry.StFips), nameof(Industry.AreaType), nameof(Industry.AreaTypeVersion), nameof(Industry.Area),
            nameof(Industry.PeriodYear), nameof(Industry.PeriodType), nameof(Industry.Period),
            nameof(Industry.Ownership), nameof(Industry.IndCodeType), nameof(Industry.IndCode));

        var resolvedFormat = downloadService.ResolveFormat(Request, format);
        if (resolvedFormat != DownloadFormat.Json)
        {
            var exportLimit = configuration.GetValue("Api:ExportRowLimit", 100_000);
            var rows = await ordered.Take(exportLimit).ToListAsync(cancellationToken);
            return resolvedFormat switch
            {
                DownloadFormat.Tsv      => downloadService.TsvResult(rows, "industry"),
                DownloadFormat.Psv      => downloadService.PsvResult(rows, "industry"),
                DownloadFormat.JsonFile => downloadService.JsonFileResult(rows, "industry"),
                DownloadFormat.Xlsx     => downloadService.XlsxResult(rows, "industry"),
                _                       => downloadService.CsvResult(rows, "industry"),
            };
        }

        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>
    /// Returns coverage metadata for the Industry table under the supplied filters.
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
        [FromQuery] string? ownership,
        [FromQuery] string? codeType,
        [FromQuery] string? indCode,
        [FromQuery] string? metadataFields,
        CancellationToken cancellationToken)
    {
        IQueryable<Industry> query = db.Industry.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));

        query = QueryFiltering.ApplyStringFilter(query, nameof(Industry.AreaType), areaType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Industry.AreaTypeVersion), areaTypeVersion);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Industry.Area), area, WidCodes.NormalizeArea);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Industry.PeriodYear), periodYear);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Industry.PeriodType), periodType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Industry.Period), period);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Industry.Ownership), ownership);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Industry.IndCodeType), codeType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Industry.IndCode), indCode, WidCodes.NormalizeCodePattern, allowWildcard: true);

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

