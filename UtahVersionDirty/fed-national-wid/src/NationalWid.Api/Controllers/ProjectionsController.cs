using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NationalWid.Api.Data;
using NationalWid.Api.Models;
using NationalWid.Api.Services;

namespace NationalWid.Api.Controllers;

/// <summary>WID 3.0 ProjectionsMatrix table (employment projections from ProjectionsCentral).</summary>
[ApiController]
[Authorize]
[Route("projections")]
public sealed class ProjectionsController(
    WIDDbContext db,
    IQueryService queryService,
    IDownloadService downloadService,
    IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// Returns a paged list of ProjectionsMatrix records matching the supplied filters.
    /// Supports CSV multi-value filters, wildcard industry/occupation codes, projection period range,
    /// user-defined sort, and download in CSV / TSV / PSV / XLSX / JSON formats.
    /// <para>
    /// <c>periodYear</c> and <c>projectedYear</c> filter on the start and end years of the projections
    /// period range respectively (e.g. <c>projectionsPeriod = "2022-2032"</c>).
    /// </para>
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
        IQueryable<ProjectionsMatrix> query = db.ProjectionsMatrix.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));
        query = QueryFiltering.ApplyStringFilter(query, nameof(ProjectionsMatrix.AreaType), areaType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(ProjectionsMatrix.AreaTypeVersion), areaTypeVersion);
        query = QueryFiltering.ApplyStringFilter(query, nameof(ProjectionsMatrix.Area), area, WidCodes.NormalizeArea);

        var resolvedMatrixIndCode = string.IsNullOrWhiteSpace(matrixIndCode) ? indCode : matrixIndCode;
        var resolvedMatrixOccCode = string.IsNullOrWhiteSpace(matrixOccCode) ? occCode : matrixOccCode;

        if (!string.IsNullOrWhiteSpace(periodYear) && string.IsNullOrWhiteSpace(projectionsPeriod))
        {
            query = query.Where(x => x.ProjectionsPeriod.StartsWith(periodYear));
        }

        if (!string.IsNullOrWhiteSpace(projectedYear))
        {
            query = query.Where(x => x.ProjectionsPeriod.EndsWith(projectedYear));
        }

        if (!string.IsNullOrWhiteSpace(periodType) && !string.Equals(periodType, "01", StringComparison.Ordinal))
        {
            query = query.Where(_ => false);
        }

        if (!string.IsNullOrWhiteSpace(period) && !string.Equals(period, "00", StringComparison.Ordinal))
        {
            query = query.Where(_ => false);
        }

        query = QueryFiltering.ApplyStringFilter(query, nameof(ProjectionsMatrix.ProjectionsPeriod), projectionsPeriod);
        query = QueryFiltering.ApplyStringFilter(query, nameof(ProjectionsMatrix.IndCodeType), indCodeType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(ProjectionsMatrix.IndCode), resolvedMatrixIndCode, WidCodes.NormalizeCodePattern, allowWildcard: true);
        query = QueryFiltering.ApplyStringFilter(query, nameof(ProjectionsMatrix.OccCodeType), occCodeType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(ProjectionsMatrix.OccCode), resolvedMatrixOccCode, WidCodes.NormalizeCodePattern, allowWildcard: true);

        var ordered = QuerySorting.Apply(
            query,
            sort,
            nameof(ProjectionsMatrix.StFips), nameof(ProjectionsMatrix.AreaType), nameof(ProjectionsMatrix.AreaTypeVersion), nameof(ProjectionsMatrix.Area),
            nameof(ProjectionsMatrix.ProjectionsPeriod),
            nameof(ProjectionsMatrix.IndCodeType), nameof(ProjectionsMatrix.IndCode), nameof(ProjectionsMatrix.OccCodeType), nameof(ProjectionsMatrix.OccCode));

        var resolvedFormat = downloadService.ResolveFormat(Request, format);
        if (resolvedFormat != DownloadFormat.Json)
        {
            var exportLimit = configuration.GetValue("Api:ExportRowLimit", 100_000);
            var rows = await ordered.Take(exportLimit).ToListAsync(cancellationToken);
            return resolvedFormat switch
            {
                DownloadFormat.Tsv      => downloadService.TsvResult(rows, "projections"),
                DownloadFormat.Psv      => downloadService.PsvResult(rows, "projections"),
                DownloadFormat.JsonFile => downloadService.JsonFileResult(rows, "projections"),
                DownloadFormat.Xlsx     => downloadService.XlsxResult(rows, "projections"),
                _                       => downloadService.CsvResult(rows, "projections"),
            };
        }

        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>Returns coverage metadata for the ProjectionsMatrix table under the supplied filters.</summary>
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
        [FromQuery] string? projectionsPeriod,
        [FromQuery] string? projectedYear,
        [FromQuery] string? indCodeType,
        [FromQuery] string? indCode,
        [FromQuery] string? matrixIndCode,
        [FromQuery] string? occCodeType,
        [FromQuery] string? occCode,
        [FromQuery] string? matrixOccCode,
        [FromQuery] string? metadataFields,
        CancellationToken cancellationToken)
    {
        IQueryable<ProjectionsMatrix> query = db.ProjectionsMatrix.AsNoTracking();

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));

        query = QueryFiltering.ApplyStringFilter(query, nameof(ProjectionsMatrix.AreaType), areaType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(ProjectionsMatrix.AreaTypeVersion), areaTypeVersion);
        query = QueryFiltering.ApplyStringFilter(query, nameof(ProjectionsMatrix.Area), area, WidCodes.NormalizeArea);

        var resolvedMatrixIndCode = string.IsNullOrWhiteSpace(matrixIndCode) ? indCode : matrixIndCode;
        var resolvedMatrixOccCode = string.IsNullOrWhiteSpace(matrixOccCode) ? occCode : matrixOccCode;

        if (!string.IsNullOrWhiteSpace(periodYear) && string.IsNullOrWhiteSpace(projectionsPeriod))
        {
            query = query.Where(x => x.ProjectionsPeriod.StartsWith(periodYear));
        }

        if (!string.IsNullOrWhiteSpace(periodType) && !string.Equals(periodType, "01", StringComparison.Ordinal))
        {
            query = query.Where(_ => false);
        }

        if (!string.IsNullOrWhiteSpace(period) && !string.Equals(period, "00", StringComparison.Ordinal))
        {
            query = query.Where(_ => false);
        }

        query = QueryFiltering.ApplyStringFilter(query, nameof(ProjectionsMatrix.ProjectionsPeriod), projectionsPeriod);
        query = QueryFiltering.ApplyStringFilter(query, nameof(ProjectionsMatrix.IndCodeType), indCodeType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(ProjectionsMatrix.IndCode), resolvedMatrixIndCode, WidCodes.NormalizeCodePattern, allowWildcard: true);
        query = QueryFiltering.ApplyStringFilter(query, nameof(ProjectionsMatrix.OccCodeType), occCodeType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(ProjectionsMatrix.OccCode), resolvedMatrixOccCode, WidCodes.NormalizeCodePattern, allowWildcard: true);

        if (!string.IsNullOrWhiteSpace(projectedYear))
        {
            query = query.Where(x => x.ProjectionsPeriod != null && x.ProjectionsPeriod.EndsWith(projectedYear));
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
                    .Select(x => x.ProjectionsPeriod)
                    .ToListAsync(cancellationToken)
            };

            metadata = metadata with
            {
                Years = metadata.Years!
                    .Select(MetadataFields.GetProjectionStartYear)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x)
                    .ToList()
            };
        }

        if (fields.Contains("projectedYears"))
        {
            var projectedYears = await query
                .Select(x => x.ProjectionsPeriod)
                .ToListAsync(cancellationToken);

            metadata = metadata with
            {
                ProjectedYears = projectedYears
                    .Select(MetadataFields.GetProjectionEndYear)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x!)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x)
                    .ToList()
            };
        }

        if (fields.Contains("minPeriod") || fields.Contains("maxPeriod"))
        {
            var spans = await query
                .Select(x => x.ProjectionsPeriod)
                .Distinct()
                .ToListAsync(cancellationToken);

            var points = spans
                .Select(span => new
                {
                    Start = MetadataFields.GetProjectionStartYear(span),
                    End = MetadataFields.GetProjectionEndYear(span),
                })
                .Where(x => !string.IsNullOrWhiteSpace(x.Start) || !string.IsNullOrWhiteSpace(x.End))
                .ToList();

            if (points.Count > 0)
            {
                if (fields.Contains("minPeriod"))
                {
                    var min = points
                        .OrderBy(x => x.Start)
                        .ThenBy(x => x.End)
                        .First();
                    metadata = metadata with
                    {
                        MinPeriod = new PeriodPoint { PeriodYear = min.Start, PeriodType = null, Period = null }
                    };
                }

                if (fields.Contains("maxPeriod"))
                {
                    var max = points
                        .OrderBy(x => x.Start)
                        .ThenBy(x => x.End)
                        .Last();
                    metadata = metadata with
                    {
                        MaxPeriod = new PeriodPoint { PeriodYear = max.End ?? max.Start, PeriodType = null, Period = null }
                    };
                }
            }
        }

        return Ok(ResponseEnvelope.CreateObject(metadata, Request));
    }

    /// <summary>
    /// Returns the Matrix Industry crosswalk (MatrixXInd), which maps projections matrix industry codes
    /// to detailed NAICS-based industry codes for the given state(s).
    /// Falls back to deriving entries from IndDirectories when no explicit crosswalk rows exist.
    /// </summary>
    [HttpGet("matrixXInd")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> GetMatrixXInd(
        [FromQuery] string? stFips,
        [FromQuery] string? matrixIndCode,
        [FromQuery] string? indCodeType,
        [FromQuery] string? indCode,
        [FromQuery] string? sort,
        [FromQuery] string? format,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        var persisted = db.MatrixXInd.AsNoTracking();
        var derived = db.IndDirectories.AsNoTracking().Select(x => new MatrixXInd
        {
            StFips = x.StFips,
            MatrixIndCode = x.IndCode,
            IndCodeType = x.IndCodeType,
            IndCode = x.IndCode,
        });

        var query = persisted
            .Concat(derived)
            .GroupBy(x => new { x.StFips, x.MatrixIndCode, x.IndCodeType, x.IndCode })
            .Select(g => new MatrixXInd
            {
                StFips = g.Key.StFips,
                MatrixIndCode = g.Key.MatrixIndCode,
                IndCodeType = g.Key.IndCodeType,
                IndCode = g.Key.IndCode,
            });

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));

        query = QueryFiltering.ApplyStringFilter(query, nameof(MatrixXInd.MatrixIndCode), matrixIndCode, WidCodes.NormalizeCodePattern, allowWildcard: true);
        query = QueryFiltering.ApplyStringFilter(query, nameof(MatrixXInd.IndCodeType), indCodeType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(MatrixXInd.IndCode), indCode, WidCodes.NormalizeCodePattern, allowWildcard: true);

        var ordered = QuerySorting.Apply(query, sort, nameof(MatrixXInd.StFips), nameof(MatrixXInd.MatrixIndCode), nameof(MatrixXInd.IndCodeType), nameof(MatrixXInd.IndCode));

        var resolvedFormat = downloadService.ResolveFormat(Request, format);
        if (resolvedFormat != DownloadFormat.Json)
        {
            var exportLimit = configuration.GetValue("Api:ExportRowLimit", 100_000);
            var rows = await ordered.Take(exportLimit).ToListAsync(cancellationToken);
            return resolvedFormat switch
            {
                DownloadFormat.Tsv      => downloadService.TsvResult(rows, "projections-matrix-x-ind"),
                DownloadFormat.Psv      => downloadService.PsvResult(rows, "projections-matrix-x-ind"),
                DownloadFormat.JsonFile => downloadService.JsonFileResult(rows, "projections-matrix-x-ind"),
                DownloadFormat.Xlsx     => downloadService.XlsxResult(rows, "projections-matrix-x-ind"),
                _                       => downloadService.CsvResult(rows, "projections-matrix-x-ind"),
            };
        }

        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }

    /// <summary>
    /// Returns the Matrix Occupation crosswalk (MatrixXOcc), which maps projections matrix occupation codes
    /// to detailed SOC-based occupation codes for the given state(s).
    /// Falls back to deriving entries from OccDirectories when no explicit crosswalk rows exist.
    /// </summary>
    [HttpGet("matrixXOcc")]
    [ResponseCache(Duration = 300, VaryByQueryKeys = new[] { "*" })]
    public async Task<IActionResult> GetMatrixXOcc(
        [FromQuery] string? stFips,
        [FromQuery] string? matrixOccCode,
        [FromQuery] string? occCodeType,
        [FromQuery] string? occCode,
        [FromQuery] string? sort,
        [FromQuery] string? format,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        [FromQuery] string? cursor,
        CancellationToken cancellationToken)
    {
        var persisted = db.MatrixXOcc.AsNoTracking();
        var derived = db.OccDirectories.AsNoTracking().Select(x => new MatrixXOcc
        {
            StFips = x.StFips,
            MatrixOccCode = x.OccCode,
            OccCodeType = x.OccCodeType,
            OccCode = x.OccCode,
        });

        var query = persisted
            .Concat(derived)
            .GroupBy(x => new { x.StFips, x.MatrixOccCode, x.OccCodeType, x.OccCode })
            .Select(g => new MatrixXOcc
            {
                StFips = g.Key.StFips,
                MatrixOccCode = g.Key.MatrixOccCode,
                OccCodeType = g.Key.OccCodeType,
                OccCode = g.Key.OccCode,
            });

        var stFipsFilter = WidCodes.ParseStFipsFilter(stFips);
        if (stFipsFilter.Length > 0)
            query = query.Where(x => stFipsFilter.Contains(x.StFips));

        query = QueryFiltering.ApplyStringFilter(query, nameof(MatrixXOcc.MatrixOccCode), matrixOccCode, WidCodes.NormalizeCodePattern, allowWildcard: true);
        query = QueryFiltering.ApplyStringFilter(query, nameof(MatrixXOcc.OccCodeType), occCodeType);
        query = QueryFiltering.ApplyStringFilter(query, nameof(MatrixXOcc.OccCode), occCode, WidCodes.NormalizeCodePattern, allowWildcard: true);

        var ordered = QuerySorting.Apply(query, sort, nameof(MatrixXOcc.StFips), nameof(MatrixXOcc.MatrixOccCode), nameof(MatrixXOcc.OccCodeType), nameof(MatrixXOcc.OccCode));

        var resolvedFormat = downloadService.ResolveFormat(Request, format);
        if (resolvedFormat != DownloadFormat.Json)
        {
            var exportLimit = configuration.GetValue("Api:ExportRowLimit", 100_000);
            var rows = await ordered.Take(exportLimit).ToListAsync(cancellationToken);
            return resolvedFormat switch
            {
                DownloadFormat.Tsv      => downloadService.TsvResult(rows, "projections-matrix-x-occ"),
                DownloadFormat.Psv      => downloadService.PsvResult(rows, "projections-matrix-x-occ"),
                DownloadFormat.JsonFile => downloadService.JsonFileResult(rows, "projections-matrix-x-occ"),
                DownloadFormat.Xlsx     => downloadService.XlsxResult(rows, "projections-matrix-x-occ"),
                _                       => downloadService.CsvResult(rows, "projections-matrix-x-occ"),
            };
        }

        var result = await queryService.PageAsync(ordered, page, pageSize, cursor, cancellationToken);
        return Ok(ResponseEnvelope.Create(result, Request));
    }
}
