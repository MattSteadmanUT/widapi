using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NationalWid.Api.Data;
using NationalWid.Api.Models;
using NationalWid.Api.Services;

namespace NationalWid.Api.Controllers;

/// <summary>
/// WID 3.0 Licensing family — authorities, licenses, history, and occupation crosswalks.
/// Licensing data covers occupational license requirements by state and maps them to occupation codes.
/// </summary>
[ApiController]
[Authorize]
[Route("licensing")]
public sealed class LicensingController(
    WIDDbContext db,
    IQueryService queryService,
    IDownloadService downloadService,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>Returns licensing authorities (LicenseAuthorities) for the specified filters.</summary>
    [HttpGet("authorities")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> Authorities(
        [FromQuery] string? stFips,
        [FromQuery] string? areaType,
        [FromQuery] string? areaTypeVersion,
        [FromQuery] string? area,
        [FromQuery] string? licAuthID,
        [FromQuery] string? sort,
        [FromQuery] string? format,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        IQueryable<LicenseAuthority> query = db.LicenseAuthorities.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));

        query = QueryFiltering.ApplyStringFilter(query, nameof(LicenseAuthority.AreaType), areaType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(LicenseAuthority.AreaTypeVersion), areaTypeVersion);
        query = QueryFiltering.ApplyStringFilter(query, nameof(LicenseAuthority.Area), area, WidCodes.NormalizeArea);
        query = QueryFiltering.ApplyStringFilter(query, nameof(LicenseAuthority.LicAuthID), licAuthID);

        var ordered = QuerySorting.Apply(query, sort,
            nameof(LicenseAuthority.StFips), nameof(LicenseAuthority.AreaType), nameof(LicenseAuthority.AreaTypeVersion),
            nameof(LicenseAuthority.Area), nameof(LicenseAuthority.LicAuthID));

        var resolvedFormat = downloadService.ResolveFormat(Request, format);
        if (resolvedFormat != DownloadFormat.Json)
        {
            var exportLimit = configuration.GetValue("Api:ExportRowLimit", 100_000);
            var rows = await ordered.Take(exportLimit).ToListAsync(cancellationToken);
            return resolvedFormat switch
            {
                DownloadFormat.Tsv      => downloadService.TsvResult(rows, "licensing-authorities"),
                DownloadFormat.Psv      => downloadService.PsvResult(rows, "licensing-authorities"),
                DownloadFormat.JsonFile => downloadService.JsonFileResult(rows, "licensing-authorities"),
                DownloadFormat.Xlsx     => downloadService.XlsxResult(rows, "licensing-authorities"),
                _                       => downloadService.CsvResult(rows, "licensing-authorities"),
            };
        }

        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>Returns license records (License) for the specified filters.</summary>
    [HttpGet("licenses")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> Licenses(
        [FromQuery] string? stFips,
        [FromQuery] string? licAuthID,
        [FromQuery] string? licenseID,
        [FromQuery] string? licenseType,
        [FromQuery] string? sort,
        [FromQuery] string? format,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        IQueryable<License> query = db.Licenses.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));

        query = QueryFiltering.ApplyStringFilter(query, nameof(License.LicAuthID), licAuthID);
        query = QueryFiltering.ApplyStringFilter(query, nameof(License.LicenseID), licenseID);
        query = QueryFiltering.ApplyStringFilter(query, nameof(License.LicenseType), licenseType);

        var ordered = QuerySorting.Apply(query, sort,
            nameof(License.StFips), nameof(License.LicAuthID), nameof(License.LicenseID), nameof(License.LicenseType));

        var resolvedFormat = downloadService.ResolveFormat(Request, format);
        if (resolvedFormat != DownloadFormat.Json)
        {
            var exportLimit = configuration.GetValue("Api:ExportRowLimit", 100_000);
            var rows = await ordered.Take(exportLimit).ToListAsync(cancellationToken);
            return resolvedFormat switch
            {
                DownloadFormat.Tsv      => downloadService.TsvResult(rows, "licensing-licenses"),
                DownloadFormat.Psv      => downloadService.PsvResult(rows, "licensing-licenses"),
                DownloadFormat.JsonFile => downloadService.JsonFileResult(rows, "licensing-licenses"),
                DownloadFormat.Xlsx     => downloadService.XlsxResult(rows, "licensing-licenses"),
                _                       => downloadService.CsvResult(rows, "licensing-licenses"),
            };
        }

        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>Returns license history records (LicenseHistory) for the specified filters.</summary>
    [HttpGet("history")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> History(
        [FromQuery] string? stFips,
        [FromQuery] string? areaType,
        [FromQuery] string? areaTypeVersion,
        [FromQuery] string? area,
        [FromQuery] string? periodYear,
        [FromQuery] string? periodType,
        [FromQuery] string? period,
        [FromQuery] string? licenseID,
        [FromQuery] string? sort,
        [FromQuery] string? format,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        IQueryable<LicenseHistory> query = db.LicenseHistory.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));

        query = QueryFiltering.ApplyStringFilter(query, nameof(LicenseHistory.AreaType), areaType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(LicenseHistory.AreaTypeVersion), areaTypeVersion);
        query = QueryFiltering.ApplyStringFilter(query, nameof(LicenseHistory.Area), area, WidCodes.NormalizeArea);
        query = QueryFiltering.ApplyStringFilter(query, nameof(LicenseHistory.PeriodYear), periodYear);
        query = QueryFiltering.ApplyStringFilter(query, nameof(LicenseHistory.PeriodType), periodType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(LicenseHistory.Period), period);
        query = QueryFiltering.ApplyStringFilter(query, nameof(LicenseHistory.LicenseID), licenseID);

        var ordered = QuerySorting.Apply(query, sort,
            nameof(LicenseHistory.StFips), nameof(LicenseHistory.AreaType), nameof(LicenseHistory.AreaTypeVersion), nameof(LicenseHistory.Area),
            nameof(LicenseHistory.PeriodYear), nameof(LicenseHistory.PeriodType), nameof(LicenseHistory.Period), nameof(LicenseHistory.LicenseID));

        var resolvedFormat = downloadService.ResolveFormat(Request, format);
        if (resolvedFormat != DownloadFormat.Json)
        {
            var exportLimit = configuration.GetValue("Api:ExportRowLimit", 100_000);
            var rows = await ordered.Take(exportLimit).ToListAsync(cancellationToken);
            return resolvedFormat switch
            {
                DownloadFormat.Tsv      => downloadService.TsvResult(rows, "licensing-history"),
                DownloadFormat.Psv      => downloadService.PsvResult(rows, "licensing-history"),
                DownloadFormat.JsonFile => downloadService.JsonFileResult(rows, "licensing-history"),
                DownloadFormat.Xlsx     => downloadService.XlsxResult(rows, "licensing-history"),
                _                       => downloadService.CsvResult(rows, "licensing-history"),
            };
        }

        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>Returns occupation crosswalk records (LicenseXOcc) mapping licenses to SOC occupation codes.</summary>
    [HttpGet("occupationCrosswalks")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> OccupationCrosswalks(
        [FromQuery] string? stFips,
        [FromQuery] string? occCodeType,
        [FromQuery] string? occCode,
        [FromQuery] string? licenseID,
        [FromQuery] string? sort,
        [FromQuery] string? format,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        IQueryable<LicenseXOcc> query = db.LicenseXOcc.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));

        query = QueryFiltering.ApplyStringFilter(query, nameof(LicenseXOcc.OccCodeType), occCodeType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(LicenseXOcc.OccCode), occCode, WidCodes.NormalizeCodePattern, allowWildcard: true);
        query = QueryFiltering.ApplyStringFilter(query, nameof(LicenseXOcc.LicenseID), licenseID);

        var ordered = QuerySorting.Apply(query, sort,
            nameof(LicenseXOcc.StFips), nameof(LicenseXOcc.OccCodeType), nameof(LicenseXOcc.OccCode), nameof(LicenseXOcc.LicenseID));

        var resolvedFormat = downloadService.ResolveFormat(Request, format);
        if (resolvedFormat != DownloadFormat.Json)
        {
            var exportLimit = configuration.GetValue("Api:ExportRowLimit", 100_000);
            var rows = await ordered.Take(exportLimit).ToListAsync(cancellationToken);
            return resolvedFormat switch
            {
                DownloadFormat.Tsv      => downloadService.TsvResult(rows, "licensing-occupation-crosswalks"),
                DownloadFormat.Psv      => downloadService.PsvResult(rows, "licensing-occupation-crosswalks"),
                DownloadFormat.JsonFile => downloadService.JsonFileResult(rows, "licensing-occupation-crosswalks"),
                DownloadFormat.Xlsx     => downloadService.XlsxResult(rows, "licensing-occupation-crosswalks"),
                _                       => downloadService.CsvResult(rows, "licensing-occupation-crosswalks"),
            };
        }

        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }
}
