using Microsoft.Extensions.Logging;
using Npgsql;

namespace NationalWid.Ingestion.Services;

public sealed class IngestLogService(ILogger logger)
{
    public async Task TryWriteAsync(
        string connectionString,
        string dataset,
        int? recordsUpserted,
        int? recordsChanged,
        int? recordsAdded,
        long? totalRecords,
        string status,
        string? errorMessage,
        DateTime? lastCheckedAt,
        DateTime? lastChangedAt,
        string? sourceUrl,
        string? sourceHash,
        bool? sourceChanged,
        long? bytesDownloaded,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var conn = new NpgsqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);

            var cmd = new NpgsqlCommand(@"
                INSERT INTO ingestlog (
                    dataset,
                    recordsupserted,
                    recordschanged,
                    recordsadded,
                    totalrecords,
                    status,
                    errormessage,
                    lastcheckedat,
                    lastchangedat,
                    sourceurl,
                    sourcehash,
                    sourcechanged,
                    bytesdownloaded)
                VALUES (
                    @dataset,
                    @recordsupserted,
                    @recordschanged,
                    @recordsadded,
                    @totalrecords,
                    @status,
                    @errormessage,
                    @lastcheckedat,
                    @lastchangedat,
                    @sourceurl,
                    @sourcehash,
                    @sourcechanged,
                    @bytesdownloaded);", conn);

            cmd.Parameters.AddWithValue("dataset", dataset);
            cmd.Parameters.AddWithValue("recordsupserted", (object?)recordsUpserted ?? DBNull.Value);
            cmd.Parameters.AddWithValue("recordschanged", (object?)recordsChanged ?? DBNull.Value);
            cmd.Parameters.AddWithValue("recordsadded", (object?)recordsAdded ?? DBNull.Value);
            cmd.Parameters.AddWithValue("totalrecords", (object?)totalRecords ?? DBNull.Value);
            cmd.Parameters.AddWithValue("status", status);
            cmd.Parameters.AddWithValue("errormessage", (object?)errorMessage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("lastcheckedat", (object?)lastCheckedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("lastchangedat", (object?)lastChangedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("sourceurl", (object?)sourceUrl ?? DBNull.Value);
            cmd.Parameters.AddWithValue("sourcehash", (object?)sourceHash ?? DBNull.Value);
            cmd.Parameters.AddWithValue("sourcechanged", (object?)sourceChanged ?? DBNull.Value);
            cmd.Parameters.AddWithValue("bytesdownloaded", (object?)bytesDownloaded ?? DBNull.Value);

            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Ingest logging should never fail the ingestion pipeline.
            logger.LogWarning(ex, "Failed to write ingestlog entry for dataset {Dataset}", dataset);
        }
    }
}