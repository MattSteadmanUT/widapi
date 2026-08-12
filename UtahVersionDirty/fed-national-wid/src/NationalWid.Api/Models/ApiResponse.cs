namespace NationalWid.Api.Models;

/// <summary>
/// Pagination metadata for the standard WID 3.0 API response envelope.
/// <c>Page</c> is null when cursor pagination was used for the request.
/// </summary>
public sealed record ApiMeta
{
    public int Total { get; init; }

    public int? Page { get; init; }

    public int PageSize { get; init; }

    public string? NextCursor { get; init; }

    public string? Message { get; init; }
}

/// <summary>Hypermedia links for the standard response envelope.</summary>
public sealed record ApiLinks
{
    public required string Self { get; init; }

    public string? Next { get; init; }
}

/// <summary>
/// Standard WID 3.0 list response envelope: <c>{ meta, data, links }</c>.
/// </summary>
public sealed record ApiResponse<T>
{
    public required ApiMeta Meta { get; init; }

    public required IReadOnlyList<T> Data { get; init; }

    public required ApiLinks Links { get; init; }
}
