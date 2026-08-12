using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NationalWid.Api.Data;
using NationalWid.Api.Models;
using NationalWid.Api.Services;

namespace NationalWid.Api.Controllers;

/// <summary>
/// Non-core WID view endpoints. These are display-oriented joins built from core and lookup tables.
/// </summary>
[ApiController]
[Authorize]
[Route("views")]
public sealed class ViewsController(
    WIDDbContext db,
    IQueryService queryService,
    IDownloadService downloadService,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Returns paginated CES (Current Employment Statistics) records joined with geography
    /// and series-code metadata.
    /// </summary>
    [HttpGet("ces-with-geography")]
    [HttpGet("cesWithGeography")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> GetCesWithGeography(
        [FromQuery] string? stFips,
        [FromQuery] string? areaType,
        [FromQuery] string? areaTypeVersion,
        [FromQuery] string? area,
        [FromQuery] string? periodYear,
        [FromQuery] string? periodType,
        [FromQuery] string? adjusted,
        [FromQuery] string? seriesCode,
        [FromQuery] string? sort,
        [FromQuery] string? format,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        IQueryable<Ces> baseQuery = db.Ces.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            baseQuery = baseQuery.Where(x => stFipsFilter.Contains(x.StFips));
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(Ces.AreaType), areaType);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(Ces.AreaTypeVersion), areaTypeVersion);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(Ces.Area), area, WidCodes.NormalizeArea);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(Ces.PeriodYear), periodYear);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(Ces.PeriodType), periodType);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(Ces.Adjusted), adjusted);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(Ces.SeriesCode), seriesCode, WidCodes.NormalizeCodePattern, allowWildcard: true);

        var query =
            from c in baseQuery
            join g in db.Geographies.AsNoTracking()
                on new { c.StFips, c.AreaType, c.AreaTypeVersion, c.Area }
                equals new { g.StFips, g.AreaType, g.AreaTypeVersion, g.Area } into gJoin
            from g in gJoin.DefaultIfEmpty()
            join code in db.CesCodes.AsNoTracking()
                on new { c.StFips, c.SeriesCodeType, c.SeriesCode }
                equals new { code.StFips, code.SeriesCodeType, code.SeriesCode } into codeJoin
            from code in codeJoin.DefaultIfEmpty()
            select new CesWithGeography
            {
                StFips = c.StFips,
                AreaType = c.AreaType,
                AreaTypeVersion = c.AreaTypeVersion,
                Area = c.Area,
                PeriodYear = c.PeriodYear,
                PeriodType = c.PeriodType,
                Period = c.Period,
                Adjusted = c.Adjusted,
                SeriesCodeType = c.SeriesCodeType,
                SeriesCode = c.SeriesCode,
                EmpCES = c.EmpCES,
                EmpProductionWorkers = c.EmpProductionWorkers,
                HoursPerWeek = c.HoursPerWeek,
                EarningsPerWeek = c.EarningsPerWeek,
                EarningsPerHour = c.EarningsPerHour,
                AvgWeeklyEarningsPctChange = c.AvgWeeklyEarningsPctChange,
                SuppRecord = c.SuppRecord,
                SuppHoursEarnings = c.SuppHoursEarnings,
                SuppProdWorkers = c.SuppProdWorkers,
                Prelim = c.Prelim,
                AreaName = g != null ? g.AreaName : null,
                SeriesTitle = code != null ? code.SeriesTitle : null,
            };

        var ordered = QuerySorting.Apply(
            query,
            sort,
            nameof(CesWithGeography.StFips), nameof(CesWithGeography.AreaType), nameof(CesWithGeography.AreaTypeVersion), nameof(CesWithGeography.Area),
            nameof(CesWithGeography.PeriodYear), nameof(CesWithGeography.PeriodType), nameof(CesWithGeography.Period),
            nameof(CesWithGeography.SeriesCodeType), nameof(CesWithGeography.SeriesCode), nameof(CesWithGeography.Adjusted),
            nameof(CesWithGeography.AreaName), nameof(CesWithGeography.SeriesTitle));

        var resolvedFormat = downloadService.ResolveFormat(Request, format);
        if (resolvedFormat != DownloadFormat.Json)
        {
            var exportLimit = configuration.GetValue("Api:ExportRowLimit", 100_000);
            var rows = await ordered.Take(exportLimit).ToListAsync(cancellationToken);
            return resolvedFormat switch
            {
                DownloadFormat.Tsv      => downloadService.TsvResult(rows, "views-ces-with-geography"),
                DownloadFormat.Psv      => downloadService.PsvResult(rows, "views-ces-with-geography"),
                DownloadFormat.JsonFile => downloadService.JsonFileResult(rows, "views-ces-with-geography"),
                DownloadFormat.Xlsx     => downloadService.XlsxResult(rows, "views-ces-with-geography"),
                _                       => downloadService.CsvResult(rows, "views-ces-with-geography"),
            };
        }

        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>Returns paginated LAUS labor-force records joined with geographic area names.</summary>
    [HttpGet("labor-force-with-geography")]
    [HttpGet("laborForceWithGeography")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> GetLaborForceWithGeography(
        [FromQuery] string? stFips,
        [FromQuery] string? areaType,
        [FromQuery] string? areaTypeVersion,
        [FromQuery] string? area,
        [FromQuery] string? periodYear,
        [FromQuery] string? periodType,
        [FromQuery] string? adjusted,
        [FromQuery] string? sort,
        [FromQuery] string? format,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        IQueryable<LaborForce> baseQuery = db.LaborForce.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            baseQuery = baseQuery.Where(x => stFipsFilter.Contains(x.StFips));
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(LaborForce.AreaType), areaType);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(LaborForce.AreaTypeVersion), areaTypeVersion);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(LaborForce.Area), area, WidCodes.NormalizeArea);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(LaborForce.PeriodYear), periodYear);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(LaborForce.PeriodType), periodType);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(LaborForce.Adjusted), adjusted);

        var query =
            from lf in baseQuery
            join g in db.Geographies.AsNoTracking()
                on new { lf.StFips, lf.AreaType, lf.AreaTypeVersion, lf.Area }
                equals new { g.StFips, g.AreaType, g.AreaTypeVersion, g.Area } into gJoin
            from g in gJoin.DefaultIfEmpty()
            select new LaborForceWithGeography
            {
                StFips = lf.StFips,
                AreaType = lf.AreaType,
                AreaTypeVersion = lf.AreaTypeVersion,
                Area = lf.Area,
                PeriodYear = lf.PeriodYear,
                PeriodType = lf.PeriodType,
                Period = lf.Period,
                Adjusted = lf.Adjusted,
                CivilianLaborForce = lf.CivilianLaborForce,
                Employed = lf.Employed,
                Unemployed = lf.Unemployed,
                UnempRate = lf.UnempRate,
                SuppRecord = lf.SuppRecord,
                SuppRate = lf.SuppRate,
                Prelim = lf.Prelim,
                AreaName = g != null ? g.AreaName : null,
            };

        var ordered = QuerySorting.Apply(
            query,
            sort,
            nameof(LaborForceWithGeography.StFips), nameof(LaborForceWithGeography.AreaType), nameof(LaborForceWithGeography.AreaTypeVersion), nameof(LaborForceWithGeography.Area),
            nameof(LaborForceWithGeography.PeriodYear), nameof(LaborForceWithGeography.PeriodType), nameof(LaborForceWithGeography.Period), nameof(LaborForceWithGeography.Adjusted),
            nameof(LaborForceWithGeography.AreaName));

        var resolvedFormat = downloadService.ResolveFormat(Request, format);
        if (resolvedFormat != DownloadFormat.Json)
        {
            var exportLimit = configuration.GetValue("Api:ExportRowLimit", 100_000);
            var rows = await ordered.Take(exportLimit).ToListAsync(cancellationToken);
            return resolvedFormat switch
            {
                DownloadFormat.Tsv      => downloadService.TsvResult(rows, "views-labor-force-with-geography"),
                DownloadFormat.Psv      => downloadService.PsvResult(rows, "views-labor-force-with-geography"),
                DownloadFormat.JsonFile => downloadService.JsonFileResult(rows, "views-labor-force-with-geography"),
                DownloadFormat.Xlsx     => downloadService.XlsxResult(rows, "views-labor-force-with-geography"),
                _                       => downloadService.CsvResult(rows, "views-labor-force-with-geography"),
            };
        }

        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>Returns paginated QCEW industry employment/wage records joined with geography and industry-title lookup.</summary>
    [HttpGet("industry-with-geography")]
    [HttpGet("industryWithGeography")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> GetIndustryWithGeography(
        [FromQuery] string? stFips,
        [FromQuery] string? areaType,
        [FromQuery] string? areaTypeVersion,
        [FromQuery] string? area,
        [FromQuery] string? periodYear,
        [FromQuery] string? periodType,
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
        IQueryable<Industry> baseQuery = db.Industry.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            baseQuery = baseQuery.Where(x => stFipsFilter.Contains(x.StFips));
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(Industry.AreaType), areaType);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(Industry.AreaTypeVersion), areaTypeVersion);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(Industry.Area), area, WidCodes.NormalizeArea);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(Industry.PeriodYear), periodYear);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(Industry.PeriodType), periodType);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(Industry.Ownership), ownership);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(Industry.IndCodeType), codeType);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(Industry.IndCode), indCode, WidCodes.NormalizeCodePattern, allowWildcard: true);

        var industryTitles = db.IndDirectories
            .AsNoTracking()
            .GroupBy(x => new { x.StFips, x.IndCodeType, x.IndCode })
            .Select(g => new
            {
                g.Key.StFips,
                g.Key.IndCodeType,
                g.Key.IndCode,
                IndustryTitle = g.Max(x => x.IndTitle),
            });

        var query =
            from ind in baseQuery
            join g in db.Geographies.AsNoTracking()
                on new { ind.StFips, ind.AreaType, ind.AreaTypeVersion, ind.Area }
                equals new { g.StFips, g.AreaType, g.AreaTypeVersion, g.Area } into gJoin
            from g in gJoin.DefaultIfEmpty()
            join indTitle in industryTitles
                on new { ind.StFips, IndCodeType = ind.IndCodeType, ind.IndCode }
                equals new { indTitle.StFips, indTitle.IndCodeType, indTitle.IndCode } into titleJoin
            from indTitle in titleJoin.DefaultIfEmpty()
            select new IndustryWithGeography
            {
                StFips = ind.StFips,
                AreaType = ind.AreaType,
                AreaTypeVersion = ind.AreaTypeVersion,
                Area = ind.Area,
                PeriodYear = ind.PeriodYear,
                PeriodType = ind.PeriodType,
                Period = ind.Period,
                Ownership = ind.Ownership,
                IndCodeType = ind.IndCodeType,
                IndCode = ind.IndCode,
                AvgMonthlyEmp = ind.AvgMonthlyEmp,
                TotalWages = ind.TotalWages,
                TaxableWages = ind.TaxableWages,
                UIContributions = ind.UIContributions,
                WeeklyWage = ind.WeeklyWage,
                EmpCount = ind.EmpCount,
                HighEmpQ = ind.HighEmpQ,
                LowEmpQ = ind.LowEmpQ,
                SuppRecord = ind.SuppRecord,
                Prelim = ind.Prelim,
                AreaName = g != null ? g.AreaName : null,
                IndustryTitle = indTitle != null ? indTitle.IndustryTitle : null,
                OwnershipTitle = null,
            };

        var ordered = QuerySorting.Apply(
            query,
            sort,
            nameof(IndustryWithGeography.StFips), nameof(IndustryWithGeography.AreaType), nameof(IndustryWithGeography.AreaTypeVersion), nameof(IndustryWithGeography.Area),
            nameof(IndustryWithGeography.PeriodYear), nameof(IndustryWithGeography.PeriodType), nameof(IndustryWithGeography.Period),
            nameof(IndustryWithGeography.Ownership), nameof(IndustryWithGeography.IndCodeType), nameof(IndustryWithGeography.IndCode),
            nameof(IndustryWithGeography.AreaName), nameof(IndustryWithGeography.IndustryTitle), nameof(IndustryWithGeography.OwnershipTitle));

        var resolvedFormat = downloadService.ResolveFormat(Request, format);
        if (resolvedFormat != DownloadFormat.Json)
        {
            var exportLimit = configuration.GetValue("Api:ExportRowLimit", 100_000);
            var rows = await ordered.Take(exportLimit).ToListAsync(cancellationToken);
            var rowsWithOwnership = rows.Select(MapOwnershipTitle).ToList();
            return resolvedFormat switch
            {
                DownloadFormat.Tsv      => downloadService.TsvResult(rowsWithOwnership, "views-industry-with-geography"),
                DownloadFormat.Psv      => downloadService.PsvResult(rowsWithOwnership, "views-industry-with-geography"),
                DownloadFormat.JsonFile => downloadService.JsonFileResult(rowsWithOwnership, "views-industry-with-geography"),
                DownloadFormat.Xlsx     => downloadService.XlsxResult(rowsWithOwnership, "views-industry-with-geography"),
                _                       => downloadService.CsvResult(rowsWithOwnership, "views-industry-with-geography"),
            };
        }

        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        var mapped = new PagedResult<IndustryWithGeography>
        {
            Items = result.Items.Select(MapOwnershipTitle).ToList(),
            Total = result.Total,
            Page = result.Page,
            PageSize = result.PageSize,
            NextCursor = result.NextCursor,
        };

        return Ok(ResponseEnvelope.Create(mapped, Request));
    }

    /// <summary>Returns paginated OES/OEWS wage records joined with geography, occupation, and industry titles.</summary>
    [HttpGet("wages-with-descriptions")]
    [HttpGet("wagesWithDescriptions")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> GetWagesWithDescriptions(
        [FromQuery] string? stFips,
        [FromQuery] string? areaType,
        [FromQuery] string? areaTypeVersion,
        [FromQuery] string? area,
        [FromQuery] string? periodYear,
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
        IQueryable<IOWage> baseQuery = db.IOWages.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            baseQuery = baseQuery.Where(x => stFipsFilter.Contains(x.StFips));
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(IOWage.AreaType), areaType);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(IOWage.AreaTypeVersion), areaTypeVersion);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(IOWage.Area), area, WidCodes.NormalizeArea);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(IOWage.PeriodYear), periodYear);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(IOWage.OccCodeType), occCodeType);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(IOWage.OccCode), occCode, WidCodes.NormalizeCodePattern, allowWildcard: true);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(IOWage.IndCodeType), indCodeType);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(IOWage.IndCode), indCode, WidCodes.NormalizeCodePattern, allowWildcard: true);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(IOWage.WageSource), wageSource);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(IOWage.RateType), rateType);

        var industryTitles = db.IndDirectories
            .AsNoTracking()
            .GroupBy(x => new { x.StFips, x.IndCodeType, x.IndCode })
            .Select(g => new
            {
                g.Key.StFips,
                g.Key.IndCodeType,
                g.Key.IndCode,
                IndustryTitle = g.Max(x => x.IndTitle),
            });

        var occupationTitles = db.OccDirectories
            .AsNoTracking()
            .GroupBy(x => new { x.StFips, x.OccCodeType, x.OccCode })
            .Select(g => new
            {
                g.Key.StFips,
                g.Key.OccCodeType,
                g.Key.OccCode,
                OccupationTitle = g.Max(x => x.OccTitle),
            });

        var query =
            from w in baseQuery
            join g in db.Geographies.AsNoTracking()
                on new { w.StFips, w.AreaType, w.AreaTypeVersion, w.Area }
                equals new { g.StFips, g.AreaType, g.AreaTypeVersion, g.Area } into gJoin
            from g in gJoin.DefaultIfEmpty()
            join indTitle in industryTitles
                on new { w.StFips, w.IndCodeType, w.IndCode }
                equals new { indTitle.StFips, indTitle.IndCodeType, indTitle.IndCode } into indTitleJoin
            from indTitle in indTitleJoin.DefaultIfEmpty()
            join occTitle in occupationTitles
                on new { w.StFips, w.OccCodeType, w.OccCode }
                equals new { occTitle.StFips, occTitle.OccCodeType, occTitle.OccCode } into occTitleJoin
            from occTitle in occTitleJoin.DefaultIfEmpty()
            select new WageWithDescriptions
            {
                StFips = w.StFips,
                AreaType = w.AreaType,
                AreaTypeVersion = w.AreaTypeVersion,
                Area = w.Area,
                PeriodYear = w.PeriodYear,
                PeriodType = w.PeriodType,
                Period = w.Period,
                OccCodeType = w.OccCodeType,
                OccCode = w.OccCode,
                IndCodeType = w.IndCodeType,
                IndCode = w.IndCode,
                WageSource = w.WageSource,
                RateType = w.RateType,
                EmpCount = w.EmpCount,
                Percentile10Wage = w.Percentile10Wage,
                Percentile25Wage = w.Percentile25Wage,
                MedianWage = w.MedianWage,
                MeanWage = w.MeanWage,
                Percentile75Wage = w.Percentile75Wage,
                Percentile90Wage = w.Percentile90Wage,
                MeanHourly = w.MeanHourly,
                AnnualMean = w.AnnualMean,
                SuppressWage = w.SuppressWage,
                AreaName = g != null ? g.AreaName : null,
                OccupationTitle = occTitle != null ? occTitle.OccupationTitle : null,
                IndustryTitle = indTitle != null ? indTitle.IndustryTitle : null,
                WageSourceTitle = null,
                RateTypeTitle = null,
            };

        var ordered = QuerySorting.Apply(
            query,
            sort,
            nameof(WageWithDescriptions.StFips), nameof(WageWithDescriptions.AreaType), nameof(WageWithDescriptions.AreaTypeVersion), nameof(WageWithDescriptions.Area),
            nameof(WageWithDescriptions.PeriodYear), nameof(WageWithDescriptions.OccCodeType), nameof(WageWithDescriptions.OccCode),
            nameof(WageWithDescriptions.IndCodeType), nameof(WageWithDescriptions.IndCode),
            nameof(WageWithDescriptions.WageSource), nameof(WageWithDescriptions.RateType),
            nameof(WageWithDescriptions.AreaName), nameof(WageWithDescriptions.OccupationTitle), nameof(WageWithDescriptions.IndustryTitle));

        var resolvedFormat = downloadService.ResolveFormat(Request, format);
        if (resolvedFormat != DownloadFormat.Json)
        {
            var exportLimit = configuration.GetValue("Api:ExportRowLimit", 100_000);
            var rows = (await ordered.Take(exportLimit).ToListAsync(cancellationToken)).Select(MapWageTitles).ToList();
            return resolvedFormat switch
            {
                DownloadFormat.Tsv      => downloadService.TsvResult(rows, "views-wages-with-descriptions"),
                DownloadFormat.Psv      => downloadService.PsvResult(rows, "views-wages-with-descriptions"),
                DownloadFormat.JsonFile => downloadService.JsonFileResult(rows, "views-wages-with-descriptions"),
                DownloadFormat.Xlsx     => downloadService.XlsxResult(rows, "views-wages-with-descriptions"),
                _                       => downloadService.CsvResult(rows, "views-wages-with-descriptions"),
            };
        }

        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        var mapped = new PagedResult<WageWithDescriptions>
        {
            Items = result.Items.Select(MapWageTitles).ToList(),
            Total = result.Total,
            Page = result.Page,
            PageSize = result.PageSize,
            NextCursor = result.NextCursor,
        };
        return Ok(ResponseEnvelope.Create(mapped, Request));
    }

    /// <summary>Returns paginated employment-projections records joined with geography, industry, and occupation titles.</summary>
    [HttpGet("projections-with-titles")]
    [HttpGet("projectionsWithTitles")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> GetProjectionsWithTitles(
        [FromQuery] string? stFips,
        [FromQuery] string? areaType,
        [FromQuery] string? areaTypeVersion,
        [FromQuery] string? area,
        [FromQuery] string? periodYear,
        [FromQuery] string? periodType,
        [FromQuery] string? period,
        [FromQuery] string? projectionsPeriod,
        [FromQuery] string? projectedYear,
        [FromQuery] string? indCodeType,
        [FromQuery] string? indCode,
        [FromQuery] string? matrixIndCode,
        [FromQuery] string? occCodeType,
        [FromQuery] string? occCode,
        [FromQuery] string? matrixOccCode,
        [FromQuery] string? sort,
        [FromQuery] string? format,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        IQueryable<ProjectionsMatrix> baseQuery = db.ProjectionsMatrix.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            baseQuery = baseQuery.Where(x => stFipsFilter.Contains(x.StFips));
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(ProjectionsMatrix.AreaType), areaType);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(ProjectionsMatrix.AreaTypeVersion), areaTypeVersion);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(ProjectionsMatrix.Area), area, WidCodes.NormalizeArea);

        var resolvedMatrixIndCode = string.IsNullOrWhiteSpace(matrixIndCode) ? indCode : matrixIndCode;
        var resolvedMatrixOccCode = string.IsNullOrWhiteSpace(matrixOccCode) ? occCode : matrixOccCode;

        if (!string.IsNullOrWhiteSpace(periodYear) && string.IsNullOrWhiteSpace(projectionsPeriod))
        {
            baseQuery = baseQuery.Where(x => x.ProjectionsPeriod.StartsWith(periodYear));
        }

        if (!string.IsNullOrWhiteSpace(projectedYear))
        {
            baseQuery = baseQuery.Where(x => x.ProjectionsPeriod.EndsWith(projectedYear));
        }

        if (!string.IsNullOrWhiteSpace(periodType) && !string.Equals(periodType, "01", StringComparison.Ordinal))
        {
            baseQuery = baseQuery.Where(_ => false);
        }

        if (!string.IsNullOrWhiteSpace(period) && !string.Equals(period, "00", StringComparison.Ordinal))
        {
            baseQuery = baseQuery.Where(_ => false);
        }

        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(ProjectionsMatrix.ProjectionsPeriod), projectionsPeriod);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(ProjectionsMatrix.IndCodeType), indCodeType);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(ProjectionsMatrix.IndCode), resolvedMatrixIndCode, WidCodes.NormalizeCodePattern, allowWildcard: true);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(ProjectionsMatrix.OccCodeType), occCodeType);
        baseQuery = QueryFiltering.ApplyStringFilter(baseQuery, nameof(ProjectionsMatrix.OccCode), resolvedMatrixOccCode, WidCodes.NormalizeCodePattern, allowWildcard: true);

        var query =
            from p in baseQuery
            join g in db.Geographies.AsNoTracking()
                on new { p.StFips, p.AreaType, p.AreaTypeVersion, p.Area }
                equals new { g.StFips, g.AreaType, g.AreaTypeVersion, g.Area } into gJoin
            from g in gJoin.DefaultIfEmpty()
            join indDir in db.IndDirectories.AsNoTracking()
                on new { p.StFips, ProjPeriod = p.ProjectionsPeriod, p.IndCodeType, p.IndCode }
                equals new { indDir.StFips, indDir.ProjPeriod, indDir.IndCodeType, indDir.IndCode } into indJoin
            from indDir in indJoin.DefaultIfEmpty()
            join occDir in db.OccDirectories.AsNoTracking()
                on new { p.StFips, ProjPeriod = p.ProjectionsPeriod, p.OccCodeType, p.OccCode }
                equals new { occDir.StFips, occDir.ProjPeriod, occDir.OccCodeType, occDir.OccCode } into occJoin
            from occDir in occJoin.DefaultIfEmpty()
            select new ProjectionWithTitles
            {
                StFips = p.StFips,
                AreaType = p.AreaType,
                AreaTypeVersion = p.AreaTypeVersion,
                Area = p.Area,
                ProjectionsPeriod = p.ProjectionsPeriod,
                IndCodeType = p.IndCodeType,
                IndCode = p.IndCode,
                OccCodeType = p.OccCodeType,
                OccCode = p.OccCode,
                BaseYearEmp = p.BaseYearEmp,
                ProjectedEmp = p.ProjectedEmp,
                Change = p.Change,
                PctChange = p.PctChange,
                Openings = p.Openings,
                SuppRecord = p.Suppress,
                AreaName = g != null ? g.AreaName : null,
                IndustryTitle = indDir != null ? indDir.IndTitle : null,
                OccupationTitle = occDir != null ? occDir.OccTitle : null,
                GrowthCodeTitle = null,
            };

        var ordered = QuerySorting.Apply(
            query,
            sort,
            nameof(ProjectionWithTitles.StFips), nameof(ProjectionWithTitles.AreaType), nameof(ProjectionWithTitles.AreaTypeVersion), nameof(ProjectionWithTitles.Area),
            nameof(ProjectionWithTitles.ProjectionsPeriod), nameof(ProjectionWithTitles.IndCodeType), nameof(ProjectionWithTitles.IndCode),
            nameof(ProjectionWithTitles.OccCodeType), nameof(ProjectionWithTitles.OccCode),
            nameof(ProjectionWithTitles.AreaName), nameof(ProjectionWithTitles.IndustryTitle), nameof(ProjectionWithTitles.OccupationTitle));

        var resolvedFormat = downloadService.ResolveFormat(Request, format);
        if (resolvedFormat != DownloadFormat.Json)
        {
            var exportLimit = configuration.GetValue("Api:ExportRowLimit", 100_000);
            var rows = await ordered.Take(exportLimit).ToListAsync(cancellationToken);
            return resolvedFormat switch
            {
                DownloadFormat.Tsv      => downloadService.TsvResult(rows, "views-projections-with-titles"),
                DownloadFormat.Psv      => downloadService.PsvResult(rows, "views-projections-with-titles"),
                DownloadFormat.JsonFile => downloadService.JsonFileResult(rows, "views-projections-with-titles"),
                DownloadFormat.Xlsx     => downloadService.XlsxResult(rows, "views-projections-with-titles"),
                _                       => downloadService.CsvResult(rows, "views-projections-with-titles"),
            };
        }

        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>Returns licensing records cross-walked to occupation codes, joined with license authority and occupation title data.</summary>
    [HttpGet("licensing-by-occupation")]
    [HttpGet("licensingByOccupation")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> GetLicensingByOccupation(
        [FromQuery] string? stFips,
        [FromQuery] string? occCodeType,
        [FromQuery] string? occCode,
        [FromQuery] string? sort,
        [FromQuery] string? format,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        var crosswalk = db.LicenseXOcc.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            crosswalk = crosswalk.Where(x => stFipsFilter.Contains(x.StFips));

        crosswalk = QueryFiltering.ApplyStringFilter(crosswalk, nameof(LicenseXOcc.OccCodeType), occCodeType);
        crosswalk = QueryFiltering.ApplyStringFilter(crosswalk, nameof(LicenseXOcc.OccCode), occCode, WidCodes.NormalizeCodePattern, allowWildcard: true);

        var occupationTitles = db.OccDirectories.AsNoTracking()
            .GroupBy(x => new { x.StFips, x.OccCodeType, x.OccCode })
            .Select(g => new
            {
                g.Key.StFips,
                g.Key.OccCodeType,
                g.Key.OccCode,
                OccupationTitle = g.Max(x => x.OccTitle),
            });

        var query =
            from x in crosswalk
            join l in db.Licenses.AsNoTracking()
                on new { x.StFips, x.LicenseID }
                equals new { l.StFips, l.LicenseID }
            join a in db.LicenseAuthorities.AsNoTracking()
                on new { l.StFips, l.LicAuthID }
                equals new { a.StFips, LicAuthID = (string?)a.LicAuthID } into authJoin
            from a in authJoin.DefaultIfEmpty()
            join o in occupationTitles
                on new { x.StFips, x.OccCodeType, x.OccCode }
                equals new { o.StFips, o.OccCodeType, o.OccCode } into occJoin
            from o in occJoin.DefaultIfEmpty()
            select new LicensingByOccupation
            {
                StFips = x.StFips,
                OccCodeType = x.OccCodeType,
                OccCode = x.OccCode,
                OccupationTitle = o != null ? o.OccupationTitle : null,
                LicenseID = x.LicenseID,
                LicenseTitle = l.LicenseTitle,
                LicenseType = l.LicenseType,
                LicAuthID = l.LicAuthID,
                Board = a != null ? a.Board : null,
                LicenseURL = l.LicenseURL,
                LicenseUpdatedDate = l.LicenseUpdatedDate,
            };

        var ordered = QuerySorting.Apply(
            query,
            sort,
            nameof(LicensingByOccupation.StFips), nameof(LicensingByOccupation.OccCodeType), nameof(LicensingByOccupation.OccCode),
            nameof(LicensingByOccupation.LicenseID), nameof(LicensingByOccupation.LicenseType), nameof(LicensingByOccupation.LicAuthID));

        var resolvedFormat = downloadService.ResolveFormat(Request, format);
        if (resolvedFormat != DownloadFormat.Json)
        {
            var exportLimit = configuration.GetValue("Api:ExportRowLimit", 100_000);
            var rows = await ordered.Take(exportLimit).ToListAsync(cancellationToken);
            return resolvedFormat switch
            {
                DownloadFormat.Tsv      => downloadService.TsvResult(rows, "views-licensing-by-occupation"),
                DownloadFormat.Psv      => downloadService.PsvResult(rows, "views-licensing-by-occupation"),
                DownloadFormat.JsonFile => downloadService.JsonFileResult(rows, "views-licensing-by-occupation"),
                DownloadFormat.Xlsx     => downloadService.XlsxResult(rows, "views-licensing-by-occupation"),
                _                       => downloadService.CsvResult(rows, "views-licensing-by-occupation"),
            };
        }

        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    private static IndustryWithGeography MapOwnershipTitle(IndustryWithGeography row) =>
        row with
        {
            OwnershipTitle = LookupCatalog.OwnershipTitles.TryGetValue(row.Ownership, out var title)
                ? title
                : row.OwnershipTitle,
        };

    private static WageWithDescriptions MapWageTitles(WageWithDescriptions row)
    {
        var wageSourceTitle = LookupCatalog.WageSources
            .FirstOrDefault(x => string.Equals(x.StFips, row.StFips, StringComparison.Ordinal)
                && string.Equals(x.WageSource, row.WageSource, StringComparison.Ordinal)).WageSourceTitle;

        var rateTypeTitle = LookupCatalog.WageRateTypes
            .FirstOrDefault(x => string.Equals(x.RateType, row.RateType, StringComparison.Ordinal)).RateTypeTitle;

        return row with
        {
            WageSourceTitle = string.IsNullOrWhiteSpace(wageSourceTitle) ? row.WageSourceTitle : wageSourceTitle,
            RateTypeTitle = string.IsNullOrWhiteSpace(rateTypeTitle) ? row.RateTypeTitle : rateTypeTitle,
        };
    }
}




