using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace NationalWid.Api.Services;

public sealed class QueryService(IConfiguration configuration) : IQueryService
{
    private readonly int _defaultPageSize = configuration.GetValue("Api:DefaultPageSize", 100);
    private readonly int _maxPageSize = configuration.GetValue("Api:MaxPageSize", 1000);

    public async Task<PagedResult<T>> PageAsync<T>(
        IQueryable<T> orderedQuery,
        int? page,
        int? pageSize,
        string? cursor,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(cursor) && page is not null)
        {
            throw new BadHttpRequestException("Specify either page or cursor, not both.");
        }

        var size = Math.Clamp(pageSize ?? _defaultPageSize, 1, _maxPageSize);
        int offset;
        int? pageOut;

        if (!string.IsNullOrWhiteSpace(cursor))
        {
            if (!CursorCodec.TryDecode(cursor, out offset, out var cursorSize))
            {
                throw new BadHttpRequestException("The supplied cursor is invalid.");
            }

            // The page size travels inside the cursor so a scan stays consistent,
            // unless the caller explicitly overrides it on this request.
            size = Math.Clamp(pageSize ?? cursorSize, 1, _maxPageSize);
            pageOut = null;
        }
        else
        {
            pageOut = Math.Max(page ?? 1, 1);
            var offsetLong = ((long)pageOut.Value - 1L) * size;
            if (offsetLong > int.MaxValue)
            {
                throw new BadHttpRequestException("The requested page is too large.");
            }

            offset = (int)offsetLong;
        }

        var total = await CountAsync(orderedQuery, cancellationToken);
        var items = await ToListAsync(orderedQuery.Skip(offset).Take(size), cancellationToken);
        var nextOffset = offset + items.Count;

        return new PagedResult<T>
        {
            Items = items,
            Total = total,
            Page = pageOut,
            PageSize = size,
            NextCursor = nextOffset < total ? CursorCodec.Encode(nextOffset, size) : null,
        };
    }

    // The async EF extensions throw on non-EF providers, so fall back to synchronous
    // LINQ-to-objects evaluation (used by unit tests over in-memory lists).
    private static Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken ct) =>
        query.Provider is IAsyncQueryProvider
            ? query.CountAsync(ct)
            : Task.FromResult(query.Count());

    private static async Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken ct) =>
        query.Provider is IAsyncQueryProvider
            ? await query.ToListAsync(ct)
            : query.ToList();
}
