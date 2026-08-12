using Microsoft.Extensions.Logging.Abstractions;
using NationalWid.Ingestion.Services;

namespace NationalWid.Ingestion.Tests;

public class BlsFlatFileServiceTests
{
    [Fact]
    public async Task TryLoadSeriesAsync_UsesTabDelimitedColumns()
    {
        var content = string.Join('\n',
            "series_id\tyear\tperiod\tvalue\tfootnote_codes",
            "LNS11000000\t2026\tM01\t1000\tP",
            "LNS12000000\t2026\tM01\t900\t");

        using var client = TestHttpClient(content);
        var service = new BlsFlatFileService(client, NullLogger.Instance);

        var result = await service.TryLoadSeriesAsync(
            dataset: "ln",
            dataFile: "ln.data.1.AllData",
            seriesIds: ["LNS11000000", "LNS12000000"],
            startYear: 2020,
            endYear: 2026,
            cancellationToken: CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Single(result.First(s => s.SeriesId == "LNS11000000").Data);
        Assert.Equal("P", result.First(s => s.SeriesId == "LNS11000000").Data[0].Footnotes[0].Code);
    }

    [Fact]
    public async Task TryLoadSeriesAsync_UsesWhitespaceFallback()
    {
        var content = string.Join('\n',
            "series_id year period value footnote_codes",
            "CES0000000001 2026 M01 155000",
            "CES0500000001 2026 M01 128000");

        using var client = TestHttpClient(content);
        var service = new BlsFlatFileService(client, NullLogger.Instance);

        var result = await service.TryLoadSeriesAsync(
            dataset: "ce",
            dataFile: "ce.data.0.AllCESSeries",
            seriesIds: ["CES0000000001"],
            startYear: 2020,
            endYear: 2026,
            cancellationToken: CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("155000", result[0].Data[0].Value);
    }

    private static HttpClient TestHttpClient(string content)
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            });

        return new HttpClient(handler);
    }
}

internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(responder(request));
}
