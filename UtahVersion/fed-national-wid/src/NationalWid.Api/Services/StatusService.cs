using Microsoft.EntityFrameworkCore;
using NationalWid.Api.Data;

namespace NationalWid.Api.Services;

public sealed record DatasetFreshness
{
    public required string DataSet { get; init; }

    public string? TableClass { get; init; }

    public DateTime? LastRunAt { get; init; }

    public DateTime? LastCheckedAt { get; init; }

    public DateTime? LastChangedAt { get; init; }

    public string? LastStatus { get; init; }

    public int? RecordsUpserted { get; init; }

    public int? RecordsChanged { get; init; }

    public int? RecordsAdded { get; init; }

    public long? TotalRecords { get; init; }

    public string? SourceUrl { get; init; }

    public string? SourceHash { get; init; }

    public bool? SourceChanged { get; init; }

    public long? BytesDownloaded { get; init; }

    public string? ErrorMessage { get; init; }
}

public sealed record DatasetActivity
{
    public required string DataSet { get; init; }

    public string? TableClass { get; init; }

    public DateTime RunAt { get; init; }

    public DateTime? LastCheckedAt { get; init; }

    public DateTime? LastChangedAt { get; init; }

    public string? LastStatus { get; init; }

    public int? RecordsUpserted { get; init; }

    public int? RecordsChanged { get; init; }

    public int? RecordsAdded { get; init; }

    public long? TotalRecords { get; init; }

    public string? SourceUrl { get; init; }

    public string? SourceHash { get; init; }

    public bool? SourceChanged { get; init; }

    public long? BytesDownloaded { get; init; }

    public string? ErrorMessage { get; init; }
}

public sealed record DatasetActivityHistory
{
    public required int Total { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required IReadOnlyList<DatasetActivity> Items { get; init; }
}

public sealed record CoreCoverageEntry
{
    public required string DataSet { get; init; }

    public required long TotalRows { get; init; }

    public required long DistinctStFips { get; init; }

    public required long DistinctAreaTypes { get; init; }

    public required IReadOnlyList<string> AreaTypes { get; init; }
}

public sealed record ServiceStatus
{
    public required string Status { get; init; }

    public required DateTime GeneratedAt { get; init; }

    public required IReadOnlyList<DatasetFreshness> Datasets { get; init; }
}

public interface IStatusService
{
    Task<ServiceStatus> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<DatasetActivityHistory> GetHistoryAsync(string? dataSet, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CoreCoverageEntry>> GetCoreCoverageAsync(CancellationToken cancellationToken = default);
}

/// <summary>Reports data freshness from the most recent ingestion-log entry per dataset.</summary>
public sealed class StatusService(WIDDbContext db, ILogger<StatusService> logger) : IStatusService
{
    private static bool HasActualDataChange(Models.IngestLog log)
        => (log.RecordsChanged ?? 0) > 0 || (log.RecordsAdded ?? 0) > 0;

    public async Task<ServiceStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Recent window is plenty: the scheduler only runs monthly per dataset.
            var recent = await db.IngestLogs
                .AsNoTracking()
                .OrderByDescending(l => l.RunAt)
                .Take(200)
                .ToListAsync(cancellationToken);

            var byCanonical = recent
                .GroupBy(l => CanonicalizeDataSet(l.DataSet))
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.RunAt).ToList(), StringComparer.OrdinalIgnoreCase);

            var datasets = new List<DatasetFreshness>();
            foreach (var catalog in TableCatalog.Entries.Where(x => x.Implemented))
            {
                byCanonical.TryGetValue(catalog.TableName, out var logs);
                var latest = logs?.FirstOrDefault();
                var latestChanged = logs?.FirstOrDefault(HasActualDataChange);
                var totalRecords = latest?.TotalRecords ?? await ResolveTotalRecordsAsync(catalog.TableName, cancellationToken);

                datasets.Add(new DatasetFreshness
                {
                    DataSet = catalog.TableName,
                    TableClass = catalog.TableClass,
                    LastRunAt = latest?.RunAt,
                    LastCheckedAt = latest?.LastCheckedAt ?? latest?.RunAt,
                    LastChangedAt = latestChanged?.LastChangedAt ?? latestChanged?.RunAt,
                    LastStatus = latest?.Status ?? "available",
                    RecordsUpserted = latest?.RecordsUpserted,
                    RecordsChanged = latest?.RecordsChanged,
                    RecordsAdded = latest?.RecordsAdded,
                    TotalRecords = totalRecords,
                    SourceUrl = latestChanged?.SourceUrl ?? latest?.SourceUrl,
                    SourceHash = latest?.SourceHash,
                    SourceChanged = latest?.SourceChanged,
                    BytesDownloaded = latest?.BytesDownloaded,
                    ErrorMessage = latest?.ErrorMessage,
                });
            }

            return new ServiceStatus
            {
                Status = "healthy",
                GeneratedAt = DateTime.UtcNow,
                Datasets = datasets
                    .OrderBy(x => x.TableClass)
                    .ThenBy(x => x.DataSet)
                    .ToList(),
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Status check could not reach the database.");
            return new ServiceStatus
            {
                Status = "degraded",
                GeneratedAt = DateTime.UtcNow,
                Datasets = [],
            };
        }
    }

    public async Task<DatasetActivityHistory> GetHistoryAsync(
        string? dataSet,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        IQueryable<Models.IngestLog> query = db.IngestLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(dataSet))
        {
            var canonical = CanonicalizeDataSet(dataSet);
            var aliases = GetAliases(canonical);
            query = query.Where(x => aliases.Contains(x.DataSet));
        }

        var total = await query.CountAsync(cancellationToken);
        var logs = await query
            .OrderByDescending(x => x.RunAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = logs.Select(x =>
        {
            var canonical = CanonicalizeDataSet(x.DataSet);
            return new DatasetActivity
            {
                DataSet = canonical,
                TableClass = ResolveTableClass(canonical),
                RunAt = x.RunAt,
                LastCheckedAt = x.LastCheckedAt ?? x.RunAt,
                LastChangedAt = HasActualDataChange(x) ? x.LastChangedAt : null,
                LastStatus = x.Status,
                RecordsUpserted = x.RecordsUpserted,
                RecordsChanged = x.RecordsChanged,
                RecordsAdded = x.RecordsAdded,
                TotalRecords = x.TotalRecords,
                SourceUrl = x.SourceUrl,
                SourceHash = x.SourceHash,
                SourceChanged = x.SourceChanged,
                BytesDownloaded = x.BytesDownloaded,
                ErrorMessage = x.ErrorMessage,
            };
        }).ToList();

        return new DatasetActivityHistory
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            Items = items,
        };
    }

    public async Task<IReadOnlyList<CoreCoverageEntry>> GetCoreCoverageAsync(CancellationToken cancellationToken = default)
    {
        var result = new List<CoreCoverageEntry>();

        async Task addCoverageAsync(
            string dataSet,
            IQueryable<string> stFipsQuery,
            IQueryable<string> areaTypeQuery,
            Func<Task<long>> totalRowsFactory)
        {
            var totalRows = await totalRowsFactory();
            var distinctStFips = await stFipsQuery.Distinct().LongCountAsync(cancellationToken);
            var distinctAreaTypes = await areaTypeQuery.Distinct().LongCountAsync(cancellationToken);
            var areaTypes = await areaTypeQuery.Distinct().OrderBy(x => x).ToListAsync(cancellationToken);

            result.Add(new CoreCoverageEntry
            {
                DataSet = dataSet,
                TotalRows = totalRows,
                DistinctStFips = distinctStFips,
                DistinctAreaTypes = distinctAreaTypes,
                AreaTypes = areaTypes,
            });
        }

        await addCoverageAsync("LaborForce", db.LaborForce.Select(x => x.StFips), db.LaborForce.Select(x => x.AreaType), () => db.LaborForce.LongCountAsync(cancellationToken));
        await addCoverageAsync("CES", db.Ces.Select(x => x.StFips), db.Ces.Select(x => x.AreaType), () => db.Ces.LongCountAsync(cancellationToken));
        await addCoverageAsync("Industry", db.Industry.Select(x => x.StFips), db.Industry.Select(x => x.AreaType), () => db.Industry.LongCountAsync(cancellationToken));
        await addCoverageAsync("IOWage", db.IOWages.Select(x => x.StFips), db.IOWages.Select(x => x.AreaType), () => db.IOWages.LongCountAsync(cancellationToken));
        await addCoverageAsync("ProjectionsMatrix", db.ProjectionsMatrix.Select(x => x.StFips), db.ProjectionsMatrix.Select(x => x.AreaType), () => db.ProjectionsMatrix.LongCountAsync(cancellationToken));

        return result;
    }

    private static string CanonicalizeDataSet(string? dataSet)
    {
        if (string.IsNullOrWhiteSpace(dataSet))
            return "Unknown";

        var trimmed = dataSet.Trim();

        var parenIndex = trimmed.IndexOf('(');
        if (parenIndex > 0)
        {
            trimmed = trimmed[..parenIndex].Trim();
        }

        return trimmed.ToUpperInvariant() switch
        {
            // Always surface canonical WID table names in /status so labels
            // remain stable even when ingestion source aliases vary.
            "CES" => "CES",
            "GEOGRAPHIES" => "Geographies",
            "AREATYPES" => "AreaTypes",
            "STATEFIPS" => "StateFips",
            "CESCODES" => "CESCodes",
            "INDDIRECTORIES" => "IndDirectories",
            "OCCDIRECTORIES" => "OccDirectories",
            "LAUS" => "LaborForce",
            "LABORFORCE" => "LaborForce",
            "QCEW" => "Industry",
            "INDUSTRY" => "Industry",
            "OES" => "IOWage",
            "OEWS" => "IOWage",
            "IOWAGE" => "IOWage",
            "PROJECTIONS" => "ProjectionsMatrix",
            "PROJECTIONSMATRIX" => "ProjectionsMatrix",
            _ => trimmed,
        };
    }

    private static string[] GetAliases(string canonicalDataSet)
    {
        return canonicalDataSet.ToUpperInvariant() switch
        {
            "LABORFORCE" => ["LaborForce", "LAUS", "LAUS (LABORFORCE)"],
            "CES" => ["CES", "CES (EMPLOYMENT)"],
            "INDUSTRY" => ["Industry", "QCEW", "QCEW (INDUSTRY)"],
            "IOWAGE" => ["IOWage", "OEWS", "OES", "OEWS (IOWAGE)"],
            "PROJECTIONSMATRIX" => ["ProjectionsMatrix", "Projections", "Projections (PROJECTIONSMATRIX)"],
            "GEOGRAPHIES" => ["Geographies", "WIDCenter-Lookups"],
            "AREATYPES" => ["AreaTypes", "WIDCenter-Lookups"],
            "STATEFIPS" => ["StateFips", "WIDCenter-Lookups"],
            "CESCODES" => ["CESCodes", "WIDCenter-Lookups"],
            "INDDIRECTORIES" => ["IndDirectories", "WIDCenter-Lookups"],
            "OCCDIRECTORIES" => ["OccDirectories", "WIDCenter-Lookups"],
            _ => [canonicalDataSet],
        };
    }

    private async Task<long?> ResolveTotalRecordsAsync(string dataSet, CancellationToken cancellationToken)
    {
        return dataSet switch
        {
            "LaborForce" => await db.LaborForce.LongCountAsync(cancellationToken),
            "CES" => await db.Ces.LongCountAsync(cancellationToken),
            "Industry" => await db.Industry.LongCountAsync(cancellationToken),
            "IOWage" => await db.IOWages.LongCountAsync(cancellationToken),
            "ProjectionsMatrix" => await db.ProjectionsMatrix.LongCountAsync(cancellationToken),
            "License" => await db.Licenses.LongCountAsync(cancellationToken),
            "LicenseAuthorities" => await db.LicenseAuthorities.LongCountAsync(cancellationToken),
            "LicenseHistory" => await db.LicenseHistory.LongCountAsync(cancellationToken),
            "LicenseXOcc" => await db.LicenseXOcc.LongCountAsync(cancellationToken),
            "Geographies" => await db.Geographies.LongCountAsync(cancellationToken),
            "AreaTypes" => await db.AreaTypeReferences.LongCountAsync(cancellationToken),
            "StateFips" => await db.StateFipsReferences.LongCountAsync(cancellationToken),
            "PeriodTypes" => await db.PeriodYears.Select(x => x.PeriodType).Distinct().LongCountAsync(cancellationToken),
            "PeriodYears" => await db.PeriodYears.LongCountAsync(cancellationToken),
            "Periods" => await db.PeriodYears.Select(x => new { x.PeriodType, x.Period }).Distinct().LongCountAsync(cancellationToken),
            "IndustryCodes" => await db.Industry.Select(x => new { x.StFips, x.IndCodeType, x.IndCode }).Distinct().LongCountAsync(cancellationToken),
            "OccupationCodes" => await db.IOWages.Select(x => new { x.StFips, x.OccCodeType, x.OccCode }).Distinct().LongCountAsync(cancellationToken),
            "CESCodes" => await db.CesCodes.LongCountAsync(cancellationToken),
            "Ownerships" => await db.Industry.Select(x => x.Ownership).Distinct().LongCountAsync(cancellationToken),
            "WageSources" => await db.IOWages.Select(x => x.WageSource).Distinct().LongCountAsync(cancellationToken),
            "WageRateTypes" => await db.IOWages.Select(x => x.RateType).Distinct().LongCountAsync(cancellationToken),
            "Benchmark" => await db.PeriodYears.Select(x => x.Year).Distinct().LongCountAsync(cancellationToken),
            "GrowthCodes" => LookupCatalog.GrowthCodes.Count,
            "IndDirectories" => await db.IndDirectories.LongCountAsync(cancellationToken),
            "OccDirectories" => await db.OccDirectories.LongCountAsync(cancellationToken),
            "MatrixXInd" => await db.MatrixXInd.LongCountAsync(cancellationToken),
            "MatrixXOcc" => await db.MatrixXOcc.LongCountAsync(cancellationToken),
            "CesWithGeography" => await db.Ces.LongCountAsync(cancellationToken),
            "LaborForceWithGeography" => await db.LaborForce.LongCountAsync(cancellationToken),
            "WageWithDescriptions" => await db.IOWages.LongCountAsync(cancellationToken),
            "ProjectionWithTitles" => await db.ProjectionsMatrix.LongCountAsync(cancellationToken),
            "IndustryWithGeography" => await db.Industry.LongCountAsync(cancellationToken),
            "LicensingByOccupation" => await db.LicenseXOcc.LongCountAsync(cancellationToken),
            "CPI" => await db.Cpi.LongCountAsync(cancellationToken),
            "CpiSeries" => await db.CpiSeries.LongCountAsync(cancellationToken),
            "CpiItems" => await db.CpiItems.LongCountAsync(cancellationToken),
            "CpiAreas" => await db.CpiAreas.LongCountAsync(cancellationToken),
            _ => null,
        };
    }

    private static string? ResolveTableClass(string dataSet) =>
        TableCatalog.Entries
            .FirstOrDefault(x => string.Equals(x.TableName, dataSet, StringComparison.OrdinalIgnoreCase))
            ?.TableClass;
}
