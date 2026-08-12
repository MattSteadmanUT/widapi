namespace NationalWid.Api.Services;

/// <summary>One page of query results plus the pagination state needed for the envelope.</summary>
public sealed record PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public required int Total { get; init; }

    /// <summary>1-based page number; null when cursor pagination was used.</summary>
    public int? Page { get; init; }

    public required int PageSize { get; init; }

    public string? NextCursor { get; init; }
}

/// <summary>
/// Generic pagination over an ordered <see cref="IQueryable{T}"/>. Supports both page-number
/// and cursor pagination; when a cursor is supplied it is authoritative and <c>page</c> is ignored.
/// </summary>
public interface IQueryService
{
    Task<PagedResult<T>> PageAsync<T>(
        IQueryable<T> orderedQuery,
        int? page,
        int? pageSize,
        string? cursor,
        CancellationToken cancellationToken = default);
}
