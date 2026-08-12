using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.SimpleSystemsManagement;
using Microsoft.Extensions.Logging;
using NationalWid.Ingestion.Ingestors;
using NationalWid.Ingestion.Services;
using Npgsql;

[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace NationalWid.Ingestion;

public sealed class Function
{
    private static readonly string[] LookupTablesTouched =
    [
        "Geographies",
        "AreaTypes",
        "StateFips",
        "CESCodes",
        "IndDirectories",
        "OccDirectories",
    ];

    private static readonly string[] ProjectionTablesTouched =
    [
        "ProjectionsMatrix",
        "OccDirectories",
    ];

    private readonly ParameterStoreService _paramStore;
    private readonly MigrationService _migrationService;
    private readonly IngestLogService _ingestLogService;
    private readonly SourceHashService _sourceHashService;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    // The deployment supplies environment-scoped paths so dev/prod stacks never
    // read or replace one another's parameters. Defaults preserve local usage.
    private static string BlsApiKeyPath => GetParameterPath(
        "BLS_API_KEY_PARAMETER",
        "/wid-api/bls-api-key");
    private static string DbConnectionStringPath => GetParameterPath(
        "DB_CONNECTION_STRING_PARAMETER",
        "/wid-api/db-connection-string");

    public Function()
    {
        _paramStore = new ParameterStoreService(new AmazonSimpleSystemsManagementClient());

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "NationalWid-Ingestion/1.0 (utah.gov)");
        _httpClient.Timeout = TimeSpan.FromMinutes(5);

        // Create logger without using statement to preserve lifetime across handler calls.
        var loggerFactory = LoggerFactory.Create(b =>
            b.AddSimpleConsole(o => o.TimestampFormat = "[HH:mm:ss] "));
        _logger = loggerFactory.CreateLogger<Function>();
        _migrationService = new MigrationService(_logger);
        _ingestLogService = new IngestLogService(_logger);
        _sourceHashService = new SourceHashService(_logger);
    }

    /// <summary>
    /// Lambda handler invoked by EventBridge Scheduler.
    /// When datasetGroup is null, runs all datasets in sequence (legacy mode, rarely used).
    /// When datasetGroup is specified, runs only that dataset group in parallel (recommended).
    /// </summary>
    public async Task<IngestSummary> FunctionHandler(IngestRequest? request, ILambdaContext context)
    {
        _logger.LogInformation("NationalWid ingestion started at {Time} (datasetGroup={DatasetGroup})", 
            DateTime.UtcNow, request?.DatasetGroup ?? "all");

        if (!string.IsNullOrWhiteSpace(request?.ForceRefreshDatasets))
        {
            Environment.SetEnvironmentVariable("WID_FORCE_SOURCE_REFRESH_DATASETS", request.ForceRefreshDatasets);
            _logger.LogInformation("Force refresh datasets enabled for this run: {Datasets}", request.ForceRefreshDatasets);
        }
        
        // Reset error counter at start of handler (warm Lambda reuse safety).
        Errors = 0;

        string connectionString;
        string? blsApiKey;

        try
        {
            connectionString = await _paramStore.GetParameterAsync(DbConnectionStringPath);
            blsApiKey = await _paramStore.GetParameterOrDefaultAsync(BlsApiKeyPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read required parameters from Parameter Store");
            throw;
        }

        var summary = new IngestSummary();

        using (var migrationCts = new CancellationTokenSource(
                   TimeSpan.FromMilliseconds(Math.Max((int)context.RemainingTime.TotalMilliseconds - 60_000, 30_000))))
        {
            if (request?.VerifyOnly == true)
            {
                summary.TableChecks = await _migrationService.VerifyOnlyAsync(connectionString, migrationCts.Token);
            }
            else if (request?.SkipMigrations != true)
            {
                var migrationResult = await _migrationService.ApplyAndVerifyAsync(connectionString, migrationCts.Token);
                summary.MigrationsApplied = migrationResult.AppliedMigrations.Count;
                summary.MigrationsSkipped = migrationResult.SkippedMigrations.Count;
                summary.TableChecks = migrationResult.TableChecks;
            }
        }

        if (request?.RunMigrationsOnly == true || request?.VerifyOnly == true)
        {
            summary.Errors = Errors;
            return summary;
        }

        // Route to the appropriate dataset group handler
        string? group = request?.DatasetGroup;
        
        if (string.IsNullOrEmpty(group) || group.Equals("lookups", StringComparison.OrdinalIgnoreCase))
        {
            await RunDatasetGroupAsync("lookups", connectionString, blsApiKey, context, summary);
        }
        if (string.IsNullOrEmpty(group) || group.Equals("laus", StringComparison.OrdinalIgnoreCase))
        {
            await RunDatasetGroupAsync("laus", connectionString, blsApiKey, context, summary);
        }
        if (string.IsNullOrEmpty(group) || group.Equals("ces", StringComparison.OrdinalIgnoreCase))
        {
            await RunDatasetGroupAsync("ces", connectionString, blsApiKey, context, summary);
        }
        if ((string.IsNullOrEmpty(group) || group.Equals("qcew", StringComparison.OrdinalIgnoreCase)) && request?.SkipHeavyDatasets != true)
        {
            await RunDatasetGroupAsync("qcew", connectionString, blsApiKey, context, summary);
        }
        if ((string.IsNullOrEmpty(group) || group.Equals("oes", StringComparison.OrdinalIgnoreCase)) && request?.SkipHeavyDatasets != true)
        {
            await RunDatasetGroupAsync("oes", connectionString, blsApiKey, context, summary);
        }
        if ((string.IsNullOrEmpty(group) || group.Equals("projections", StringComparison.OrdinalIgnoreCase)) && request?.SkipHeavyDatasets != true)
        {
            await RunDatasetGroupAsync("projections", connectionString, blsApiKey, context, summary);
        }
        if (string.IsNullOrEmpty(group) || group.Equals("cpi", StringComparison.OrdinalIgnoreCase))
        {
            await RunDatasetGroupAsync("cpi", connectionString, blsApiKey, context, summary);
        }

        summary.Errors = Errors;

        _logger.LogInformation(
                "Ingestion complete. Lookups={Lookups} LAUS={Laus} CES={Ces} QCEW={Qcew} OES={Oes} Projections={Projections} CPI={Cpi} Errors={Errors}",
                summary.Lookups, summary.Laus, summary.Ces, summary.Qcew, summary.Oes, summary.Projections, summary.Cpi, summary.Errors);

        return summary;
    }

    private async Task RunDatasetGroupAsync(
        string group,
        string connectionString,
        string? blsApiKey,
        ILambdaContext context,
        IngestSummary summary)
    {
        switch (group.ToLowerInvariant())
        {
            case "lookups":
                {
                    var before = await CaptureTableCountsAsync(connectionString, LookupTablesTouched);
                    var result = await RunSafeAsync("WIDCenter-Lookups", async ct =>
                    {
                        var ingestor = new WidCenterLookupIngestor(_httpClient, connectionString, _sourceHashService, _logger);
                        return await ingestor.RunAsync(ct);
                    }, () => (int)context.RemainingTime.TotalMilliseconds);
                    summary.Lookups = result.Rows;
                    await WriteTableLogsAsync(connectionString, LookupTablesTouched, before, result);
                    break;
                }
            case "laus":
                {
                    var before = await CaptureTableCountsAsync(connectionString, ["LaborForce"]);
                    var result = await RunSafeAsync("LAUS", async ct =>
                    {
                        var ingestor = new BlsLausIngestor(_httpClient, blsApiKey, connectionString, _sourceHashService, _logger);
                        return await ingestor.RunAsync(ct);
                    }, () => (int)context.RemainingTime.TotalMilliseconds);
                    summary.Laus = result.Rows;
                    await WriteTableLogsAsync(connectionString, ["LaborForce"], before, result);
                    break;
                }
            case "ces":
                {
                    var before = await CaptureTableCountsAsync(connectionString, ["CES"]);
                    var result = await RunSafeAsync("CES", async ct =>
                    {
                        var ingestor = new BlsCesIngestor(_httpClient, blsApiKey, connectionString, _sourceHashService, _logger);
                        return await ingestor.RunAsync(ct);
                    }, () => (int)context.RemainingTime.TotalMilliseconds);
                    summary.Ces = result.Rows;
                    await WriteTableLogsAsync(connectionString, ["CES"], before, result);
                    break;
                }
            case "qcew":
                {
                    var before = await CaptureTableCountsAsync(connectionString, ["Industry"]);
                    var result = await RunSafeAsync("QCEW", async ct =>
                    {
                        var ingestor = new BlsQcewIngestor(_httpClient, connectionString, _sourceHashService, _logger);
                        return await ingestor.RunAsync(ct);
                    }, () => (int)context.RemainingTime.TotalMilliseconds);
                    summary.Qcew = result.Rows;
                    await WriteTableLogsAsync(connectionString, ["Industry"], before, result);
                    break;
                }
            case "oes":
                {
                    var before = await CaptureTableCountsAsync(connectionString, ["IOWage"]);
                    var result = await RunSafeAsync("OEWS", async ct =>
                    {
                        var ingestor = new BlsOesIngestor(_httpClient, connectionString, _sourceHashService, _logger);
                        return await ingestor.RunAsync(ct);
                    }, () => (int)context.RemainingTime.TotalMilliseconds);
                    summary.Oes = result.Rows;
                    await WriteTableLogsAsync(connectionString, ["IOWage"], before, result);
                    break;
                }
            case "projections":
                {
                    var before = await CaptureTableCountsAsync(connectionString, ProjectionTablesTouched);
                    var result = await RunSafeAsync("Projections", async ct =>
                    {
                        var ingestor = new BlsProjectionsIngestor(_httpClient, connectionString, _sourceHashService, _logger);
                        return await ingestor.RunAsync(ct);
                    }, () => (int)context.RemainingTime.TotalMilliseconds);
                    summary.Projections = result.Rows;
                    await WriteTableLogsAsync(connectionString, ProjectionTablesTouched, before, result);
                    break;
                }
            case "cpi":
                {
                    var before = await CaptureTableCountsAsync(connectionString, ["CPI"]);
                    var result = await RunSafeAsync("CPI", async ct =>
                    {
                        var ingestor = new BlsCpiIngestor(_httpClient, connectionString, _sourceHashService, _logger);
                        return await ingestor.RunAsync(ct);
                    }, () => (int)context.RemainingTime.TotalMilliseconds);
                    summary.Cpi = result.Rows;
                    await WriteTableLogsAsync(connectionString, ["CPI"], before, result);
                    break;
                }
        }
    }

    private async Task<DatasetRunResult> RunSafeAsync(
        string dataSet,
        Func<CancellationToken, Task<int>> ingestor,
        Func<int> getRemainingMs)
    {
        // Reserve 30 s for cleanup / remaining ingestors.
        var remainingMs = getRemainingMs();  // ms as int
        using var cts = new CancellationTokenSource(
            TimeSpan.FromMilliseconds(Math.Max(remainingMs - 30_000, 30_000)));

        try
        {
            _logger.LogInformation("{DataSet}: starting", dataSet);
            var count = await ingestor(cts.Token);
            _logger.LogInformation("{DataSet}: completed — {Count} rows upserted", dataSet, count);
            return new DatasetRunResult(count, "ok", null);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("{DataSet}: cancelled (Lambda timeout approaching)", dataSet);
            return new DatasetRunResult(-1, "cancelled", "Lambda timeout approaching");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "{DataSet}: failed", dataSet);
            Errors++;
            return new DatasetRunResult(-1, "failed", ex.Message);
        }
    }

    private async Task<Dictionary<string, long?>> CaptureTableCountsAsync(string connectionString, IEnumerable<string> tableNames)
    {
        var counts = new Dictionary<string, long?>(StringComparer.OrdinalIgnoreCase);
        foreach (var table in tableNames)
        {
            counts[table] = await TryGetTableCountAsync(connectionString, table);
        }

        return counts;
    }

    private async Task<long?> TryGetTableCountAsync(string connectionString, string tableName)
    {
        var physicalTable = tableName.ToLowerInvariant() switch
        {
            "laborforce" => "laborforce",
            "ces" => "ces",
            "industry" => "industry",
            "iowage" => "iowage",
            "projectionsmatrix" => "projectionsmatrix",
            "geographies" => "geographies",
            "areatypes" => "areatypes",
            "statefips" => "statefips",
            "cescodes" => "cescodes",
            "inddirectories" => "inddirectories",
            "occdirectories" => "occdirectories",
            "cpi" => "cpi",
            _ => null,
        };

        if (physicalTable is null)
            return null;

        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();
            var cmd = new NpgsqlCommand($"SELECT COUNT(*)::bigint FROM {physicalTable};", conn);
            var value = await cmd.ExecuteScalarAsync();
            return value is long count ? count : Convert.ToInt64(value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to count rows for table {TableName}", tableName);
            return null;
        }
    }

    private async Task<SourceObservation?> GetLatestSourceObservationAsync(string connectionString, string tableName)
    {
        var sourceDatasets = GetSourceDatasetsForTable(tableName).ToArray();
        if (sourceDatasets.Length == 0)
            return null;

        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync();

            var detailCmd = new NpgsqlCommand(@"
                SELECT dataset, sourceurl, sourcehash, sourcechanged, bytesdownloaded, retrievedat, errormessage
                FROM ingestdetail
                WHERE dataset = ANY(@datasets)
                ORDER BY retrievedat DESC
                LIMIT 1;", conn);
            detailCmd.Parameters.AddWithValue("datasets", sourceDatasets);

            string? dataset = null;
            string? sourceUrl = null;
            string? sourceHash = null;
            bool? sourceChanged = null;
            long? bytesDownloaded = null;
            string? errorMessage = null;

            await using (var reader = await detailCmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    dataset = reader.IsDBNull(0) ? null : reader.GetString(0);
                    sourceUrl = reader.IsDBNull(1) ? null : reader.GetString(1);
                    sourceHash = reader.IsDBNull(2) ? null : reader.GetString(2);
                    sourceChanged = reader.IsDBNull(3) ? null : reader.GetBoolean(3);
                    bytesDownloaded = reader.IsDBNull(4) ? null : reader.GetInt64(4);
                    errorMessage = reader.IsDBNull(6) ? null : reader.GetString(6);
                }
            }

            var hashCmd = new NpgsqlCommand(@"
                SELECT MAX(lastseenat), MAX(lastchangedat)
                FROM ingestsourcehash
                WHERE dataset = ANY(@datasets);", conn);
            hashCmd.Parameters.AddWithValue("datasets", sourceDatasets);

            DateTime? lastCheckedAt = null;
            DateTime? lastChangedAt = null;
            await using (var reader = await hashCmd.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    lastCheckedAt = reader.IsDBNull(0) ? null : reader.GetDateTime(0);
                    lastChangedAt = reader.IsDBNull(1) ? null : reader.GetDateTime(1);
                }
            }

            return new SourceObservation
            {
                Dataset = dataset,
                SourceUrl = sourceUrl,
                SourceHash = sourceHash,
                SourceChanged = sourceChanged,
                BytesDownloaded = bytesDownloaded,
                ErrorMessage = errorMessage,
                LastCheckedAt = lastCheckedAt,
                LastChangedAt = lastChangedAt,
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read source observation for table {TableName}", tableName);
            return null;
        }
    }

    private IEnumerable<string> GetSourceDatasetsForTable(string tableName)
    {
        return tableName.ToLowerInvariant() switch
        {
            "laborforce" => ["BLS-FLAT-LN", "BLS-FLAT-LA", "BLS-API-LAUS", "BLS-API-LAUS-STATE"],
            "ces" => ["BLS-FLAT-CE", "BLS-FLAT-SM", "BLS-API-CES", "BLS-API-CES-STATE"],
            "industry" => ["BLS-QCEW"],
            "iowage" => ["BLS-OEWS"],
            "projectionsmatrix" => ["BLS-PROJECTIONS"],
            "geographies" => ["WIDCENTER-LOOKUPS"],
            "areatypes" => ["WIDCENTER-LOOKUPS"],
            "statefips" => ["WIDCENTER-LOOKUPS"],
            "cescodes" => ["WIDCENTER-LOOKUPS"],
            "inddirectories" => ["WIDCENTER-LOOKUPS"],
            "occdirectories" => ["WIDCENTER-LOOKUPS", "BLS-PROJECTIONS"],
            _ => [],
        };
    }

    private async Task WriteTableLogsAsync(
        string connectionString,
        IEnumerable<string> tableNames,
        Dictionary<string, long?> beforeCounts,
        DatasetRunResult result)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        foreach (var tableName in tableNames)
        {
            var before = beforeCounts.TryGetValue(tableName, out var beforeCount) ? beforeCount : null;
            var after = await TryGetTableCountAsync(connectionString, tableName);
            var source = await GetLatestSourceObservationAsync(connectionString, tableName);

            int? changed = result.Rows >= 0 ? result.Rows : null;
            int? added = null;
            if (before.HasValue && after.HasValue)
            {
                var delta = after.Value - before.Value;
                added = delta > 0 ? (int)Math.Min(delta, int.MaxValue) : 0;
            }

            await _ingestLogService.TryWriteAsync(
                connectionString,
                tableName,
                result.Rows >= 0 ? result.Rows : null,
                changed,
                added,
                after,
                result.Status,
                result.ErrorMessage ?? source?.ErrorMessage,
                source?.LastCheckedAt,
                source?.LastChangedAt,
                source?.SourceUrl,
                source?.SourceHash,
                source?.SourceChanged,
                source?.BytesDownloaded,
                cts.Token);
        }
    }

    private int Errors { get; set; }

    internal static string GetParameterPath(string environmentVariable, string fallback)
    {
        var configured = Environment.GetEnvironmentVariable(environmentVariable);
        return string.IsNullOrWhiteSpace(configured) ? fallback : configured.Trim();
    }
}

public sealed class IngestRequest
{
    public bool RunMigrationsOnly { get; set; }
    public bool VerifyOnly { get; set; }
    public bool SkipMigrations { get; set; }
    public string? ForceRefreshDatasets { get; set; }
    /// <summary>When true, skips QCEW, OES, and Projections (run daily for LAUS/CES timeliness).</summary>
    public bool SkipHeavyDatasets { get; set; }
    /// <summary>When specified, runs only this dataset group in parallel mode (lookups, laus, ces, qcew, oes, projections, cpi).</summary>
    public string? DatasetGroup { get; set; }
}

public sealed class IngestSummary
{
    public int Lookups { get; set; }
    public int Laus { get; set; }
    public int Ces { get; set; }
    public int Qcew { get; set; }
    public int Oes { get; set; }
    public int Projections { get; set; }
    public int Cpi { get; set; }
    public int MigrationsApplied { get; set; }
    public int MigrationsSkipped { get; set; }
    public Dictionary<string, bool> TableChecks { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int Errors { get; set; }
}

internal sealed record DatasetRunResult(int Rows, string Status, string? ErrorMessage);

internal sealed record SourceObservation
{
    public string? Dataset { get; init; }

    public string? SourceUrl { get; init; }

    public string? SourceHash { get; init; }

    public bool? SourceChanged { get; init; }

    public long? BytesDownloaded { get; init; }

    public string? ErrorMessage { get; init; }

    public DateTime? LastCheckedAt { get; init; }

    public DateTime? LastChangedAt { get; init; }
}
