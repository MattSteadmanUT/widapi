using NationalWid.Api.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

namespace NationalWid.Api.Tests;

public sealed class QueryServiceTests
{
    private static readonly IConfiguration Config = new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Api:DefaultPageSize"] = "100",
            ["Api:MaxPageSize"] = "1000",
        })
        .Build();

    [Fact]
    public async Task PageAsync_ThrowsWhenPageAndCursorAreBothProvided()
    {
        var service = new QueryService(Config);
        var query = Enumerable.Range(1, 10).AsQueryable();
        var cursor = CursorCodec.Encode(0, 5);

        var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() =>
            service.PageAsync(query, page: 2, pageSize: 5, cursor: cursor, CancellationToken.None));

        Assert.Contains("either page or cursor", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PageAsync_ThrowsWhenPageOffsetWouldOverflow()
    {
        var service = new QueryService(Config);
        var query = Enumerable.Range(1, 10).AsQueryable();

        var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() =>
            service.PageAsync(query, page: int.MaxValue, pageSize: 1000, cursor: null, CancellationToken.None));

        Assert.Contains("too large", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
