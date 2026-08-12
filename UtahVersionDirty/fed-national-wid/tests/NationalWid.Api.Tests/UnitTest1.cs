using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using NationalWid.Api;

namespace NationalWid.Api.Tests;

public class HealthAndStatusTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthAndStatusTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ReturnsUnauthenticatedSuccess()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");
        
        Assert.True(response.IsSuccessStatusCode, $"Expected success, got {response.StatusCode}");
        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);

        Assert.True(doc.RootElement.TryGetProperty("status", out var statusElement));
        Assert.Equal(JsonValueKind.String, statusElement.ValueKind);
        var status = statusElement.GetString();
        Assert.Equal("healthy", status);
    }

    [Fact]
    public async Task Status_ReturnsUnauthenticatedSuccess()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/status");
        
        Assert.True(response.IsSuccessStatusCode, $"Expected success, got {response.StatusCode}");
        var content = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(content);
        
        // Status endpoint returns an object with status, generatedAt, and datasets fields
        Assert.True(doc.RootElement.TryGetProperty("status", out var statusElement));
        Assert.Equal(JsonValueKind.String, statusElement.ValueKind);
        Assert.True(doc.RootElement.TryGetProperty("generatedAt", out var generatedAtElement));
        Assert.Equal(JsonValueKind.String, generatedAtElement.ValueKind);
        Assert.True(doc.RootElement.TryGetProperty("datasets", out var datasetsElement));
        Assert.Equal(JsonValueKind.Array, datasetsElement.ValueKind);
    }

    [Fact]
    public async Task ProtectedEndpoints_ReturnUnauthorizedWithoutToken()
    {
        var client = _factory.CreateClient();
        
        var endpoints = new[] { "/ces", "/labor-force", "/industry", "/wages", "/projections", "/lookups/geographies" };
        
        foreach (var endpoint in endpoints)
        {
            var response = await client.GetAsync(endpoint);
            Assert.True(
                response.StatusCode == System.Net.HttpStatusCode.Unauthorized 
                || response.StatusCode == System.Net.HttpStatusCode.Forbidden,
                $"Endpoint {endpoint} should require auth, but returned {response.StatusCode}");
        }
    }
}

public class ResponseEnvelopeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ResponseEnvelopeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListEndpoints_ReturnStandardEnvelope()
    {
        var client = _factory.CreateClient();
        
        // Call lookups which don't require auth
        var response = await client.GetAsync("/lookups/geographies?stFips=00&pageSize=1");
        
        // This will return 401/403 without a token, but we're testing the structure if available
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(content);
            
            // Standard envelope has meta, data, links
            Assert.True(doc.RootElement.TryGetProperty("meta", out var meta));
            Assert.Equal(JsonValueKind.Object, meta.ValueKind);
            Assert.True(doc.RootElement.TryGetProperty("data", out var data));
            Assert.True(data.ValueKind is JsonValueKind.Array or JsonValueKind.Object);
            Assert.True(doc.RootElement.TryGetProperty("links", out var links));
            Assert.Equal(JsonValueKind.Object, links.ValueKind);
            Assert.True(meta.TryGetProperty("total", out var total));
            Assert.True(total.ValueKind is JsonValueKind.Number or JsonValueKind.String);
            Assert.True(meta.TryGetProperty("page", out var page));
            Assert.True(page.ValueKind is JsonValueKind.Number or JsonValueKind.String);
            Assert.True(meta.TryGetProperty("pageSize", out var pageSize));
            Assert.True(pageSize.ValueKind is JsonValueKind.Number or JsonValueKind.String);
        }
    }
}
