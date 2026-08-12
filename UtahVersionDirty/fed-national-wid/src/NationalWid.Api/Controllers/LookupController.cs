using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NationalWid.Api.Data;
using NationalWid.Api.Models;
using NationalWid.Api.Services;

namespace NationalWid.Api.Controllers;

/// <summary>
/// Shared WID 3.0 lookup tables. Requires authentication like the data endpoints —
/// lookups reveal what data is loaded, so they are not anonymous.
/// </summary>
[ApiController]
[Authorize]
[Route("lookups")]
public sealed class LookupController(
    WIDDbContext db,
    IQueryService queryService) : ControllerBase
{
    /// <summary>Returns paginated geographic area records, optionally filtered by state FIPS, area type, version, or area code.</summary>
    [HttpGet("geographies")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> Geographies(
        [FromQuery] string? stFips,
        [FromQuery] string? areaType,
        [FromQuery] string? areaTypeVersion,
        [FromQuery] string? area,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        IQueryable<Geography> query = db.Geographies.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));
        query = QueryFiltering.ApplyStringFilter(query, nameof(Geography.AreaType), areaType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Geography.AreaTypeVersion), areaTypeVersion);
        query = QueryFiltering.ApplyStringFilter(query, nameof(Geography.Area), area, WidCodes.NormalizeArea);

        var ordered = QuerySorting.Apply(
            query,
            sort,
            nameof(Geography.StFips), nameof(Geography.AreaType), nameof(Geography.AreaTypeVersion), nameof(Geography.Area));
        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>Returns a single geography record by its composite primary-key route parameters.</summary>
    [HttpGet("geographies/{stFips}/{areaType}/{areaTypeVersion}/{area}")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> GeographyByKey(
        [FromRoute] string stFips,
        [FromRoute] string areaType,
        [FromRoute] string areaTypeVersion,
        [FromRoute] string area,
        CancellationToken cancellationToken)
    {
        var normalizedStFips = WidCodes.NormalizeStFips(stFips);
        if (string.IsNullOrWhiteSpace(normalizedStFips) || !WidCodes.IsValidStFips(normalizedStFips))
            throw new BadHttpRequestException("stFips must be a 2-digit numeric code.");

        var normalizedArea = WidCodes.NormalizeArea(area) ?? area;

        var row = await db.Geographies
            .AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.StFips == normalizedStFips
                && x.AreaType == areaType
                && x.AreaTypeVersion == areaTypeVersion
                && x.Area == normalizedArea,
                cancellationToken);

        return row is null ? NotFound() : Ok(row);
    }

    /// <summary>Returns valid data years per state and period type.</summary>
    [HttpGet("period-years")]
    [HttpGet("periodYears")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> PeriodYears(
        [FromQuery] string? stFips,
        [FromQuery] string? periodType,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        IQueryable<PeriodYear> query = db.PeriodYears.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));
        query = QueryFiltering.ApplyStringFilter(query, nameof(PeriodYear.PeriodType), periodType);

        var ordered = QuerySorting.Apply(query, sort, nameof(PeriodYear.StFips), nameof(PeriodYear.Year), nameof(PeriodYear.PeriodType), nameof(PeriodYear.Period));
        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>Returns CES series-code records, optionally filtered by state FIPS, series-code type, or series code.</summary>
    [HttpGet("ces-codes")]
    [HttpGet("cesCodes")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> CesCodes(
        [FromQuery] string? stFips,
        [FromQuery] string? seriesCodeType,
        [FromQuery] string? seriesCode,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        IQueryable<CesCode> query = db.CesCodes.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));
        query = QueryFiltering.ApplyStringFilter(query, nameof(CesCode.SeriesCodeType), seriesCodeType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(CesCode.SeriesCode), seriesCode, WidCodes.NormalizeCodePattern, allowWildcard: true);

        var ordered = QuerySorting.Apply(query, sort, nameof(CesCode.StFips), nameof(CesCode.SeriesCodeType), nameof(CesCode.SeriesCode));
        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>Returns distinct area-type descriptors, with a fallback to the Geographies table when the AreaTypeReferences table is empty.</summary>
    [HttpGet("area-types")]
    [HttpGet("areaTypes")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> AreaTypes(
        [FromQuery] string? stFips,
        [FromQuery] string? areaType,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        var query = db.AreaTypeReferences
            .AsNoTracking()
            .Select(x => new AreaTypeLookup
            {
                StFips = x.StFips,
                AreaType = x.AreaType,
                AreaTypeName = x.AreaTypeName,
            })
            .Distinct();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));

        query = QueryFiltering.ApplyStringFilter(query, nameof(AreaTypeLookup.AreaType), areaType);

        var ordered = QuerySorting.Apply(query, sort, nameof(AreaTypeLookup.StFips), nameof(AreaTypeLookup.AreaType));
        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);

        if (result.Total > 0)
        {
            return Ok(ResponseEnvelope.Create(result, Request));
        }

        var fallbackQuery = db.Geographies
            .AsNoTracking()
            .Select(x => new AreaTypeLookup
            {
                StFips = x.StFips,
                AreaType = x.AreaType,
                AreaTypeName = x.AreaTypeTitle,
            })
            .Distinct();

        fallbackQuery = QueryFiltering.ApplyStringFilter(fallbackQuery, nameof(AreaTypeLookup.AreaType), areaType);

        if (stFipsFilter.Length > 0)
            fallbackQuery = fallbackQuery.Where(x => stFipsFilter.Contains(x.StFips));

        var fallbackOrdered = QuerySorting.Apply(fallbackQuery, sort, nameof(AreaTypeLookup.StFips), nameof(AreaTypeLookup.AreaType));
        var fallbackResult = await queryService.PageAsync(fallbackOrdered, page, pageSize, cursor, cancellationToken);

        var mapped = new PagedResult<AreaTypeLookup>
        {
            Items = fallbackResult.Items.Select(x => new AreaTypeLookup
            {
                StFips = x.StFips,
                AreaType = x.AreaType,
                AreaTypeName = string.IsNullOrWhiteSpace(x.AreaTypeName)
                    ? (LookupCatalog.AreaTypeTitles.TryGetValue(x.AreaType, out var title) ? title : null)
                    : x.AreaTypeName,
            }).ToList(),
            Total = fallbackResult.Total,
            Page = fallbackResult.Page,
            PageSize = fallbackResult.PageSize,
            NextCursor = fallbackResult.NextCursor,
        };

        return Ok(ResponseEnvelope.Create(mapped, Request));
    }

    /// <summary>Returns state FIPS code descriptors, with a fallback to the static catalog when the StateFipsReferences table is empty.</summary>
    [HttpGet("state-fips")]
    [HttpGet("stateFips")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> StateFips(
        [FromQuery] string? stFips,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        var query = db.StateFipsReferences
            .AsNoTracking()
            .Select(x => new StateFipsLookup
            {
                StFips = x.StFips,
                StateName = x.StateName,
                StateAbbrev = x.StateAbbrev,
            });

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));

        var ordered = QuerySorting.Apply(query, sort, nameof(StateFipsLookup.StFips), nameof(StateFipsLookup.StateName));
        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);

        if (result.Total > 0)
        {
            return Ok(ResponseEnvelope.Create(result, Request));
        }

        var fallbackQuery = LookupCatalog.StateFips
            .Select(x => new StateFipsLookup
            {
                StFips = x.Key,
                StateName = x.Value.Name,
                StateAbbrev = x.Value.Abbrev,
            })
            .AsQueryable();
        if (stFipsFilter.Length > 0)
            fallbackQuery = fallbackQuery.Where(x => stFipsFilter.Contains(x.StFips));

        var fallbackOrdered = QuerySorting.Apply(fallbackQuery, sort, nameof(StateFipsLookup.StFips), nameof(StateFipsLookup.StateName));
        var fallbackResult = await queryService.PageAsync(fallbackOrdered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(fallbackResult, Request));
    }

    /// <summary>Returns the distinct period types present in the PeriodYears table, mapped to human-readable titles.</summary>
    [HttpGet("period-types")]
    [HttpGet("periodTypes")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> PeriodTypes(
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        var query = db.PeriodYears
            .AsNoTracking()
            .Select(x => x.PeriodType)
            .Distinct()
            .Select(x => new PeriodTypeLookup
            {
                PeriodType = x,
                PeriodTypeName = x,
            });

        var ordered = QuerySorting.Apply(query, sort, nameof(PeriodTypeLookup.PeriodType));
        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);

        var mapped = new PagedResult<PeriodTypeLookup>
        {
            Items = result.Items.Select(x => new PeriodTypeLookup
            {
                PeriodType = x.PeriodType,
                PeriodTypeName = LookupCatalog.PeriodTypeTitles.TryGetValue(x.PeriodType, out var title) ? title : x.PeriodType,
            }).ToList(),
            Total = result.Total,
            Page = result.Page,
            PageSize = result.PageSize,
            NextCursor = result.NextCursor,
        };

        return Ok(ResponseEnvelope.Create(mapped, Request));
    }

    /// <summary>Returns distinct period codes, optionally filtered by period type.</summary>
    [HttpGet("periods")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> Periods(
        [FromQuery] string? periodType,
        [FromQuery] string? period,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        var query = db.PeriodYears
            .AsNoTracking()
            .Select(x => new PeriodLookup
            {
                PeriodType = x.PeriodType,
                Period = x.Period,
            })
            .Distinct();

        query = QueryFiltering.ApplyStringFilter(query, nameof(PeriodLookup.PeriodType), periodType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(PeriodLookup.Period), period);

        var ordered = QuerySorting.Apply(query, sort, nameof(PeriodLookup.PeriodType), nameof(PeriodLookup.Period));
        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>Returns distinct industry codes aggregated across the Industry, IOWages, ProjectionsMatrix, and IndDirectories tables.</summary>
    [HttpGet("industry-codes")]
    [HttpGet("industryCodes")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> IndustryCodes(
        [FromQuery] string? stFips,
        [FromQuery] string? codeType,
        [FromQuery] string? code,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        var fromIndustry = db.Industry.AsNoTracking().Select(x => new IndustryCodeLookup
        {
            StFips = x.StFips,
            CodeType = x.IndCodeType,
            Code = x.IndCode,
            CodeLevel = null,
            CodeTitle = null,
            CodeTitleLong = null,
        });
        var fromWages = db.IOWages.AsNoTracking().Select(x => new IndustryCodeLookup
        {
            StFips = x.StFips,
            CodeType = x.IndCodeType,
            Code = x.IndCode,
            CodeLevel = null,
            CodeTitle = null,
            CodeTitleLong = null,
        });
        var fromProjections = db.ProjectionsMatrix.AsNoTracking().Select(x => new IndustryCodeLookup
        {
            StFips = x.StFips,
            CodeType = x.IndCodeType,
            Code = x.IndCode,
            CodeLevel = null,
            CodeTitle = null,
            CodeTitleLong = null,
        });
        var fromIndDirectories = db.IndDirectories.AsNoTracking().Select(x => new IndustryCodeLookup
        {
            StFips = x.StFips,
            CodeType = x.IndCodeType,
            Code = x.IndCode,
            CodeLevel = null,
            CodeTitle = x.IndTitle,
            CodeTitleLong = x.IndTitle,
        });

        var query = fromIndustry
            .Concat(fromWages)
            .Concat(fromProjections)
            .Concat(fromIndDirectories)
            .GroupBy(x => new { x.StFips, x.CodeType, x.Code })
            .Select(g => new IndustryCodeLookup
            {
                StFips = g.Key.StFips,
                CodeType = g.Key.CodeType,
                Code = g.Key.Code,
                CodeTitle = g.Max(x => x.CodeTitle),
                CodeTitleLong = g.Max(x => x.CodeTitleLong),
                CodeLevel = g.Max(x => x.CodeLevel),
            });

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));

        query = QueryFiltering.ApplyStringFilter(query, nameof(IndustryCodeLookup.CodeType), codeType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IndustryCodeLookup.Code), code, WidCodes.NormalizeCodePattern, allowWildcard: true);

        var ordered = QuerySorting.Apply(query, sort, nameof(IndustryCodeLookup.StFips), nameof(IndustryCodeLookup.CodeType), nameof(IndustryCodeLookup.Code));
        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>Returns distinct occupation codes aggregated across the IOWages, ProjectionsMatrix, and OccDirectories tables.</summary>
    [HttpGet("occupation-codes")]
    [HttpGet("occupationCodes")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> OccupationCodes(
        [FromQuery] string? stFips,
        [FromQuery] string? codeType,
        [FromQuery] string? code,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        var fromWages = db.IOWages.AsNoTracking().Select(x => new OccupationCodeLookup
        {
            StFips = x.StFips,
            CodeType = x.OccCodeType,
            Code = x.OccCode,
            CodeLevel = null,
            CodeTitle = null,
            CodeTitleLong = null,
            SubTotal = null,
        });
        var fromProjections = db.ProjectionsMatrix.AsNoTracking().Select(x => new OccupationCodeLookup
        {
            StFips = x.StFips,
            CodeType = x.OccCodeType,
            Code = x.OccCode,
            CodeLevel = null,
            CodeTitle = null,
            CodeTitleLong = null,
            SubTotal = null,
        });
        var fromOccDirectories = db.OccDirectories.AsNoTracking().Select(x => new OccupationCodeLookup
        {
            StFips = x.StFips,
            CodeType = x.OccCodeType,
            Code = x.OccCode,
            CodeLevel = null,
            CodeTitle = x.OccTitle,
            CodeTitleLong = x.OccTitle,
            SubTotal = null,
        });

        var query = fromWages
            .Concat(fromProjections)
            .Concat(fromOccDirectories)
            .GroupBy(x => new { x.StFips, x.CodeType, x.Code })
            .Select(g => new OccupationCodeLookup
            {
                StFips = g.Key.StFips,
                CodeType = g.Key.CodeType,
                Code = g.Key.Code,
                CodeTitle = g.Max(x => x.CodeTitle),
                CodeTitleLong = g.Max(x => x.CodeTitleLong),
                CodeLevel = g.Max(x => x.CodeLevel),
                SubTotal = g.Max(x => x.SubTotal),
            });

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));

        query = QueryFiltering.ApplyStringFilter(query, nameof(OccupationCodeLookup.CodeType), codeType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(OccupationCodeLookup.Code), code, WidCodes.NormalizeCodePattern, allowWildcard: true);

        var ordered = QuerySorting.Apply(query, sort, nameof(OccupationCodeLookup.StFips), nameof(OccupationCodeLookup.CodeType), nameof(OccupationCodeLookup.Code));
        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>Returns distinct ownership codes from the Industry table, mapped to human-readable titles.</summary>
    [HttpGet("ownerships")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> Ownerships(
        [FromQuery] string? ownership,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        var query = db.Industry
            .AsNoTracking()
            .Select(x => x.Ownership)
            .Distinct()
            .Select(x => new OwnershipLookup
            {
                Ownership = x,
                OwnershipTitle = null,
            });

        query = QueryFiltering.ApplyStringFilter(query, nameof(OwnershipLookup.Ownership), ownership);

        var ordered = QuerySorting.Apply(query, sort, nameof(OwnershipLookup.Ownership));
        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);

        var mapped = new PagedResult<OwnershipLookup>
        {
            Items = result.Items.Select(x => new OwnershipLookup
            {
                Ownership = x.Ownership,
                OwnershipTitle = LookupCatalog.OwnershipTitles.TryGetValue(x.Ownership, out var title) ? title : null,
            }).ToList(),
            Total = result.Total,
            Page = result.Page,
            PageSize = result.PageSize,
            NextCursor = result.NextCursor,
        };

        return Ok(ResponseEnvelope.Create(mapped, Request));
    }

    /// <summary>Returns wage-source codes and titles from the static catalog, optionally filtered by state FIPS or wage-source code.</summary>
    [HttpGet("wage-sources")]
    [HttpGet("wageSources")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> WageSources(
        [FromQuery] string? stFips,
        [FromQuery] string? wageSource,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        var query = LookupCatalog.WageSources
            .Select(x => new WageSourceLookup
            {
                StFips = x.StFips,
                WageSource = x.WageSource,
                WageSourceTitle = x.WageSourceTitle,
            })
            .AsQueryable();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));

        query = QueryFiltering.ApplyStringFilter(query, nameof(WageSourceLookup.WageSource), wageSource);

        var ordered = QuerySorting.Apply(query, sort, nameof(WageSourceLookup.StFips), nameof(WageSourceLookup.WageSource));
        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>Returns wage rate-type codes and titles from the static catalog.</summary>
    [HttpGet("wage-rate-types")]
    [HttpGet("wageRateTypes")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> WageRateTypes(
        [FromQuery] string? rateType,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        var query = LookupCatalog.WageRateTypes
            .Select(x => new WageRateTypeLookup
            {
                RateType = x.RateType,
                RateTypeTitle = x.RateTypeTitle,
            })
            .AsQueryable();

        query = QueryFiltering.ApplyStringFilter(query, nameof(WageRateTypeLookup.RateType), rateType);

        var ordered = QuerySorting.Apply(query, sort, nameof(WageRateTypeLookup.RateType));
        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>Returns distinct benchmark years from the PeriodYears table.</summary>
    [HttpGet("benchmarks")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> Benchmarks(
        [FromQuery] string? benchmark,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        var query = db.PeriodYears
            .AsNoTracking()
            .Select(x => x.Year)
            .Distinct()
            .Select(x => new BenchmarkLookup
            {
                Benchmark = x,
            });

        query = QueryFiltering.ApplyStringFilter(query, nameof(BenchmarkLookup.Benchmark), benchmark);

        var ordered = QuerySorting.Apply(query, sort, nameof(BenchmarkLookup.Benchmark));
        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>Returns employment growth-code descriptors from the static catalog, optionally filtered by state FIPS or growth code.</summary>
    [HttpGet("growth-codes")]
    [HttpGet("growthCodes")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> GrowthCodes(
        [FromQuery] string? stFips,
        [FromQuery] string? growthCode,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        var query = LookupCatalog.GrowthCodes
            .Select(x => new GrowthCodeLookup
            {
                StFips = x.StFips,
                GrowthCode = x.GrowthCode,
                GrowthCodeTitle = x.GrowthCodeTitle,
            })
            .AsQueryable();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));

        query = QueryFiltering.ApplyStringFilter(query, nameof(GrowthCodeLookup.GrowthCode), growthCode);

        var ordered = QuerySorting.Apply(query, sort, nameof(GrowthCodeLookup.StFips), nameof(GrowthCodeLookup.GrowthCode));
        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>Returns industry-directory records for projections cycles, optionally filtered by state FIPS, projection period, code type, or code.</summary>
    [HttpGet("ind-directories")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> IndDirectories(
        [FromQuery] string? stFips,
        [FromQuery] string? projPeriod,
        [FromQuery] string? indCodeType,
        [FromQuery] string? indCode,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        IQueryable<IndDirectory> query = db.IndDirectories.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));
        query = QueryFiltering.ApplyStringFilter(query, nameof(IndDirectory.ProjPeriod), projPeriod);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IndDirectory.IndCodeType), indCodeType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(IndDirectory.IndCode), indCode, WidCodes.NormalizeCodePattern, allowWildcard: true);

        var ordered = QuerySorting.Apply(
            query,
            sort,
            nameof(IndDirectory.StFips), nameof(IndDirectory.ProjPeriod), nameof(IndDirectory.IndCodeType), nameof(IndDirectory.IndCode));
        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>Returns occupation-directory records for projections cycles, optionally filtered by state FIPS, projection period, code type, or code.</summary>
    [HttpGet("occ-directories")]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> OccDirectories(
        [FromQuery] string? stFips,
        [FromQuery] string? projPeriod,
        [FromQuery] string? occCodeType,
        [FromQuery] string? occCode,
        [FromQuery] string? sort,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        IQueryable<OccDirectory> query = db.OccDirectories.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));
        query = QueryFiltering.ApplyStringFilter(query, nameof(OccDirectory.ProjPeriod), projPeriod);
        query = QueryFiltering.ApplyStringFilter(query, nameof(OccDirectory.OccCodeType), occCodeType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(OccDirectory.OccCode), occCode, WidCodes.NormalizeCodePattern, allowWildcard: true);

        var ordered = QuerySorting.Apply(
            query,
            sort,
            nameof(OccDirectory.StFips), nameof(OccDirectory.ProjPeriod), nameof(OccDirectory.OccCodeType), nameof(OccDirectory.OccCode));
        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }
}





