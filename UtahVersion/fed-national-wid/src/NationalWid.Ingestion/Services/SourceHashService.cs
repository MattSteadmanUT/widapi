using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace NationalWid.Ingestion.Services;

public sealed class SourceHashService(ILogger logger)
{
    public static string ComputeSha256Hex(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static async Task<string> ComputeFileSha256HexAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public async Task<bool> ShouldProcessAsync(
        string connectionString,
        string dataset,
        string sourceUrl,
        string sourceHash,
        long? bytesDownloaded,
        CancellationToken cancellationToken)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var tx = await conn.BeginTransactionAsync(cancellationToken);

        var selectCmd = new NpgsqlCommand(@"
            SELECT sourcehash
            FROM ingestsourcehash
            WHERE dataset = @dataset AND sourceurl = @sourceurl
            FOR UPDATE;", conn, tx);
        selectCmd.Parameters.AddWithValue("dataset", dataset);
        selectCmd.Parameters.AddWithValue("sourceurl", sourceUrl);

        var existing = await selectCmd.ExecuteScalarAsync(cancellationToken) as string;
        var changed = !string.Equals(existing, sourceHash, StringComparison.OrdinalIgnoreCase);
        var forceRefreshDatasets = ParseForceRefreshDatasets();
        var forceRefresh = forceRefreshDatasets.Contains("ALL") || forceRefreshDatasets.Contains(dataset.ToUpperInvariant());
        var shouldProcess = changed || forceRefresh;

        if (existing is null)
        {
            var insertCache = new NpgsqlCommand(@"
                INSERT INTO ingestsourcehash (dataset, sourceurl, sourcehash, lastchangedat, lastseenat)
                VALUES (@dataset, @sourceurl, @sourcehash, now(), now());", conn, tx);
            insertCache.Parameters.AddWithValue("dataset", dataset);
            insertCache.Parameters.AddWithValue("sourceurl", sourceUrl);
            insertCache.Parameters.AddWithValue("sourcehash", sourceHash);
            await insertCache.ExecuteNonQueryAsync(cancellationToken);
        }
        else if (changed)
        {
            var updateCache = new NpgsqlCommand(@"
                UPDATE ingestsourcehash
                SET sourcehash = @sourcehash,
                    lastchangedat = now(),
                    lastseenat = now()
                WHERE dataset = @dataset AND sourceurl = @sourceurl;", conn, tx);
            updateCache.Parameters.AddWithValue("dataset", dataset);
            updateCache.Parameters.AddWithValue("sourceurl", sourceUrl);
            updateCache.Parameters.AddWithValue("sourcehash", sourceHash);
            await updateCache.ExecuteNonQueryAsync(cancellationToken);
        }
        else
        {
            var touchCache = new NpgsqlCommand(@"
                UPDATE ingestsourcehash
                SET lastseenat = now()
                WHERE dataset = @dataset AND sourceurl = @sourceurl;", conn, tx);
            touchCache.Parameters.AddWithValue("dataset", dataset);
            touchCache.Parameters.AddWithValue("sourceurl", sourceUrl);
            await touchCache.ExecuteNonQueryAsync(cancellationToken);
        }

        var detailCmd = new NpgsqlCommand(@"
            INSERT INTO ingestdetail (dataset, sourceurl, sourcehash, sourcechanged, bytesdownloaded)
            VALUES (@dataset, @sourceurl, @sourcehash, @sourcechanged, @bytesdownloaded);", conn, tx);
        detailCmd.Parameters.AddWithValue("dataset", dataset);
        detailCmd.Parameters.AddWithValue("sourceurl", sourceUrl);
        detailCmd.Parameters.AddWithValue("sourcehash", sourceHash);
        detailCmd.Parameters.AddWithValue("sourcechanged", changed);
        detailCmd.Parameters.AddWithValue("bytesdownloaded", (object?)bytesDownloaded ?? DBNull.Value);
        await detailCmd.ExecuteNonQueryAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);

        if (!changed && !forceRefresh)
        {
            logger.LogInformation("{Dataset}: source unchanged, skipping update for {SourceUrl}", dataset, sourceUrl);
        }
        else if (!changed && forceRefresh)
        {
            logger.LogInformation("{Dataset}: force refresh enabled, processing unchanged source {SourceUrl}", dataset, sourceUrl);
        }

        return shouldProcess;
    }

    private static HashSet<string> ParseForceRefreshDatasets()
    {
        var raw = Environment.GetEnvironmentVariable("WID_FORCE_SOURCE_REFRESH_DATASETS");
        if (string.IsNullOrWhiteSpace(raw))
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
