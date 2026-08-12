using NationalWid.Ingestion.Ingestors;

namespace NationalWid.Ingestion.Tests;

public sealed class BlsCpiIngestorTests
{
    [Fact]
    public void ResolveBaseUrl_UsesDefaultCuPath_WhenSettingMissing()
    {
        var baseUrl = BlsCpiIngestor.ResolveBaseUrl(null);

        Assert.Equal("https://download.bls.gov/pub/time.series/cu", baseUrl);
    }

    [Fact]
    public void ResolveBaseUrl_AppendsCuSegment_WhenConfiguredWithSharedFlatFileRoot()
    {
        var baseUrl = BlsCpiIngestor.ResolveBaseUrl("https://download.bls.gov/pub/time.series");

        Assert.Equal("https://download.bls.gov/pub/time.series/cu", baseUrl);
    }

    [Fact]
    public void ResolveBaseUrl_PreservesExistingCuSegment()
    {
        var baseUrl = BlsCpiIngestor.ResolveBaseUrl("https://download.bls.gov/pub/time.series/cu/");

        Assert.Equal("https://download.bls.gov/pub/time.series/cu", baseUrl);
    }
}
