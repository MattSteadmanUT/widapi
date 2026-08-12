using Microsoft.Extensions.Logging;
using Npgsql;

namespace NationalWid.Ingestion.Services;

public sealed class MigrationService(ILogger logger)
{
    private const string MigrationHistoryTable = "wid_migration_history";

    private static readonly string[] RequiredTables =
    [
        "laborforce",
        "ces",
        "industry",
        "iowage",
        "projectionsmatrix",
        "matrixxind",
        "matrixxocc",
        "licenseauthorities",
        "license",
        "licensehistory",
        "licensexocc",
        "geographies",
        "areatypes",
        "statefips",
        "periodyears",
        "ingestlog",
        "ingestsourcehash",
        "ingestdetail",
    ];

    internal static string[] GetRequiredTables() => RequiredTables;

    internal static IReadOnlyList<string> GetOrderedMigrationFiles(string migrationDirectory)
    {
        if (!Directory.Exists(migrationDirectory))
            throw new DirectoryNotFoundException($"Migration directory not found: {migrationDirectory}");

        return Directory
            .GetFiles(migrationDirectory, "*.sql", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public async Task<MigrationResult> ApplyAndVerifyAsync(
        string connectionString,
        CancellationToken cancellationToken,
        string? migrationDirectory = null)
    {
        var directory = migrationDirectory ?? Path.Combine(AppContext.BaseDirectory, "migrations");
        var files = GetOrderedMigrationFiles(directory);

        var result = new MigrationResult
        {
            MigrationDirectory = directory,
            TotalMigrations = files.Count,
        };

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        await EnsureMigrationHistoryTableAsync(conn, cancellationToken);

        foreach (var file in files)
        {
            var scriptName = Path.GetFileName(file);

            if (await IsMigrationAppliedAsync(conn, scriptName, cancellationToken))
            {
                result.SkippedMigrations.Add(scriptName);
                continue;
            }

            var sql = await File.ReadAllTextAsync(file, cancellationToken);

            await using var tx = await conn.BeginTransactionAsync(cancellationToken);
            try
            {
                var command = new NpgsqlCommand(sql, conn, tx);
                await command.ExecuteNonQueryAsync(cancellationToken);

                var insert = new NpgsqlCommand(
                    $"INSERT INTO {MigrationHistoryTable} (script_name) VALUES (@script_name)",
                    conn,
                    tx);
                insert.Parameters.AddWithValue("script_name", scriptName);
                await insert.ExecuteNonQueryAsync(cancellationToken);

                await tx.CommitAsync(cancellationToken);
                result.AppliedMigrations.Add(scriptName);
                logger.LogInformation("Migration applied: {ScriptName}", scriptName);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        }

        result.TableChecks = await VerifyRequiredTablesAsync(conn, cancellationToken);
        return result;
    }

    public async Task<Dictionary<string, bool>> VerifyOnlyAsync(string connectionString, CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);
        return await VerifyRequiredTablesAsync(conn, cancellationToken);
    }

    private static async Task EnsureMigrationHistoryTableAsync(NpgsqlConnection conn, CancellationToken cancellationToken)
    {
        var command = new NpgsqlCommand($@"
            CREATE TABLE IF NOT EXISTS {MigrationHistoryTable} (
                script_name varchar(255) PRIMARY KEY,
                applied_at timestamptz NOT NULL DEFAULT now()
            );", conn);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<bool> IsMigrationAppliedAsync(
        NpgsqlConnection conn,
        string scriptName,
        CancellationToken cancellationToken)
    {
        var command = new NpgsqlCommand(
            $"SELECT EXISTS (SELECT 1 FROM {MigrationHistoryTable} WHERE script_name = @script_name)",
            conn);
        command.Parameters.AddWithValue("script_name", scriptName);

        return (bool)(await command.ExecuteScalarAsync(cancellationToken) ?? false);
    }

    private static async Task<Dictionary<string, bool>> VerifyRequiredTablesAsync(
        NpgsqlConnection conn,
        CancellationToken cancellationToken)
    {
        var checks = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        foreach (var table in RequiredTables)
        {
            var command = new NpgsqlCommand("SELECT to_regclass(@table_name)::text", conn);
            command.Parameters.AddWithValue("table_name", $"public.{table}");
            var exists = await command.ExecuteScalarAsync(cancellationToken) is string;
            checks[table] = exists;
        }

        return checks;
    }
}

public sealed class MigrationResult
{
    public string MigrationDirectory { get; set; } = string.Empty;
    public int TotalMigrations { get; set; }
    public List<string> AppliedMigrations { get; set; } = [];
    public List<string> SkippedMigrations { get; set; } = [];
    public Dictionary<string, bool> TableChecks { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}