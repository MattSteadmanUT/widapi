namespace NationalWid.Api.Models;

/// <summary>
/// Operational log of BLS ingestion runs (administrative table, not part of WID 3.0 core).
/// </summary>
public sealed record IngestLog
{
    public long Id { get; init; }

    /// <summary>Dataset identifier, e.g. 'LAUS', 'CES', 'QCEW', 'OES'.</summary>
    public string DataSet { get; init; } = "";

    public DateTime RunAt { get; init; }

    public int? RecordsUpserted { get; init; }

    public int? RecordsChanged { get; init; }

    public int? RecordsAdded { get; init; }

    public long? TotalRecords { get; init; }

    /// <summary>'Succeeded' or 'Failed'.</summary>
    public string Status { get; init; } = "";

    public string? ErrorMessage { get; init; }

    public DateTime? LastCheckedAt { get; init; }

    public DateTime? LastChangedAt { get; init; }

    public string? SourceUrl { get; init; }

    public string? SourceHash { get; init; }

    public bool? SourceChanged { get; init; }

    public long? BytesDownloaded { get; init; }
}
