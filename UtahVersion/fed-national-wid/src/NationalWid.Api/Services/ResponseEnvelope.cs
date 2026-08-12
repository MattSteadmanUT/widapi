using Microsoft.AspNetCore.WebUtilities;
using NationalWid.Api.Models;

namespace NationalWid.Api.Services;

/// <summary>Builds the standard <c>{ meta, data, links }</c> envelope from a paged result.</summary>
public static class ResponseEnvelope
{
    public static ApiResponse<T> Create<T>(PagedResult<T> page, HttpRequest request)
    {
        var self = request.Path + request.QueryString;

        string? next = null;
        if (page.NextCursor is not null)
        {
            // The next link always uses cursor pagination for stable traversal,
            // regardless of how the current page was requested.
            var query = request.Query
                .Where(kvp => !string.Equals(kvp.Key, "page", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(kvp.Key, "cursor", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString());
            query["cursor"] = page.NextCursor;
            next = QueryHelpers.AddQueryString(request.Path, query!);
        }

        return new ApiResponse<T>
        {
            Meta = new ApiMeta
            {
                Total = page.Total,
                Page = page.Page,
                PageSize = page.PageSize,
                NextCursor = page.NextCursor,
                Message = page.Total == 0 ? "No records found with the supplied filters." : null,
            },
            Data = page.Items,
            Links = new ApiLinks { Self = self, Next = next },
        };
    }

    public static ApiObjectResponse<T> CreateObject<T>(T data, HttpRequest request, string? message = null)
    {
        var self = request.Path + request.QueryString;
        return new ApiObjectResponse<T>
        {
            Meta = new ApiMeta
            {
                Total = 1,
                Page = null,
                PageSize = 1,
                NextCursor = null,
                Message = message,
            },
            Data = data,
            Links = new ApiLinks { Self = self, Next = null },
        };
    }
}
