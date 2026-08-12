using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NationalWid.Api.Data;
using NationalWid.Api.Models;
using NationalWid.Api.Services;

namespace NationalWid.Api.Controllers;

/// <summary>WID 3.0 IOWage table (OES/OEWS occupational wages).</summary>
[ApiController]
[Authorize]
[Route("wages")]
public sealed class WagesController(
    WIDDbContext db,
    IQueryService queryService,
    IDownloadService downloadService,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Returns a paged list of IOWage (OEWS occupational wage) records matching the supplied filters.
    /// Supports CSV multi-value filters, wildcard occupation/industry codes, min/max year range,
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
        [FromQuery] string? occCodeType,
        [FromQuery] string? occCode,
        [FromQuery] string? indCodeType,
        [FromQuery] string? indCode,
        [FromQuery] string? wageSource,
        [FromQuery] string? rateType,
        [FromQuery] string? sort,
        [FromQuery] string? format,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        IQueryable<IOWage> query = db.IOWages.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.AreaType), areaType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.AreaTypeVersion), areaTypeVersion);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.Area), area, WidCodes.NormalizeArea);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.PeriodYear), periodYear);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.PeriodType), periodType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.Period), period);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.OccCodeType), occCodeType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.OccCode), occCode, WidCodes.NormalizeCodePattern, allowWildcard: true);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.IndCodeType), indCodeType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.IndCode), indCode, WidCodes.NormalizeCodePattern, allowWildcard: true);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.WageSource), wageSource);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.RateType), rateType);

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
            nameof(IOWage.StFips), nameof(IOWage.AreaType), nameof(IOWage.AreaTypeVersion), nameof(IOWage.Area),
            nameof(IOWage.PeriodYear), nameof(IOWage.PeriodType), nameof(IOWage.Period),
            nameof(IOWage.IndCodeType), nameof(IOWage.IndCode), nameof(IOWage.OccCodeType), nameof(IOWage.OccCode),
            nameof(IOWage.WageSource), nameof(IOWage.RateType));

        var resolvedFormat = downloadService.ResolveFormat(Request, format);
        if (resolvedFormat != DownloadFormat.Json)
        {
            var exportLimit = configuration.GetValue("Api:ExportRowLimit", 100_000);
            var rows = await ordered.Take(exportLimit).ToListAsync(cancellationToken);
            return resolvedFormat switch
            {
                DownloadFormat.Tsv      => downloadService.TsvResult(rows, "wages"),
                DownloadFormat.Psv      => downloadService.PsvResult(rows, "wages"),
                DownloadFormat.JsonFile => downloadService.JsonFileResult(rows, "wages"),
                DownloadFormat.Xlsx     => downloadService.XlsxResult(rows, "wages"),
                _                       => downloadService.CsvResult(rows, "wages"),
            };
        }

        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>
    /// Returns coverage metadata for the IOWage table under the supplied filters.
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
        [FromQuery] string? occCodeType,
        [FromQuery] string? occCode,
        [FromQuery] string? indCodeType,
        [FromQuery] string? indCode,
        [FromQuery] string? wageSource,
        [FromQuery] string? rateType,
        [FromQuery] string? metadataFields,
        CancellationToken cancellationToken)
    {
        IQueryable<IOWage> query = db.IOWages.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));

        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.AreaType), areaType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.AreaTypeVersion), areaTypeVersion);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.Area), area, WidCodes.NormalizeArea);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.PeriodYear), periodYear);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.PeriodType), periodType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.Period), period);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.OccCodeType), occCodeType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.OccCode), occCode, WidCodes.NormalizeCodePattern, allowWildcard: true);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.IndCodeType), indCodeType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.IndCode), indCode, WidCodes.NormalizeCodePattern, allowWildcard: true);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.WageSource), wageSource);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IOWage.RateType), rateType);

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
