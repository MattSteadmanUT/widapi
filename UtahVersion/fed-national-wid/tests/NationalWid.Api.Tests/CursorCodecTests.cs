using NationalWid.Api.Services;

namespace NationalWid.Api.Tests;

public class CursorCodecTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(0, 100)]
    [InlineData(12345, 250)]
    public void EncodeThenTryDecode_RoundTripsValues(int offset, int pageSize)
    {
        var cursor = CursorCodec.Encode(offset, pageSize);

        var decoded = CursorCodec.TryDecode(cursor, out var decodedOffset, out var decodedPageSize);

        Assert.True(decoded);
        Assert.Equal(offset, decodedOffset);
        Assert.Equal(pageSize, decodedPageSize);
    }

    [Fact]
    public void Encode_ReturnsBase64UrlWithoutPadding()
    {
        var cursor = CursorCodec.Encode(42, 7);

        Assert.DoesNotContain('=', cursor);
        Assert.DoesNotContain('+', cursor);
        Assert.DoesNotContain('/', cursor);
        Assert.Matches("^[A-Za-z0-9_-]+$", cursor);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void TryDecode_ReturnsFalse_ForNullOrWhitespace(string? cursor)
    {
        var decoded = CursorCodec.TryDecode(cursor, out var offset, out var pageSize);

        Assert.False(decoded);
        Assert.Equal(0, offset);
        Assert.Equal(0, pageSize);
    }

    [Theory]
    [InlineData("!!!")]
    [InlineData("not-base64")]
    [InlineData("a")]
    public void TryDecode_ReturnsFalse_ForInvalidBase64(string cursor)
    {
        var decoded = CursorCodec.TryDecode(cursor, out _, out _);

        Assert.False(decoded);
    }

    [Theory]
    [InlineData("{\"o\":-1,\"s\":10}")]
    [InlineData("{\"o\":1,\"s\":0}")]
    [InlineData("{\"o\":1,\"s\":-10}")]
    [InlineData("{\"s\":0}")]
    [InlineData("[]")]
    [InlineData("\"text\"")]
    [InlineData("{bad-json}")]
    public void TryDecode_ReturnsFalse_ForInvalidPayload(string payloadJson)
    {
        var cursor = ToBase64Url(payloadJson);

        var decoded = CursorCodec.TryDecode(cursor, out _, out _);

        Assert.False(decoded);
    }

    [Fact]
    public void TryDecode_AllowsMissingOffsetField()
    {
        var cursor = ToBase64Url("{\"S\":50}");

        var decoded = CursorCodec.TryDecode(cursor, out var offset, out var pageSize);

        Assert.True(decoded);
        Assert.Equal(0, offset);
        Assert.Equal(50, pageSize);
    }

    [Fact]
    public void TryDecode_ReturnsFalse_WhenPageSizeFieldIsMissing()
    {
        var cursor = ToBase64Url("{\"O\":99}");

        var decoded = CursorCodec.TryDecode(cursor, out var offset, out var pageSize);

        Assert.False(decoded);
        Assert.Equal(0, offset);
        Assert.Equal(0, pageSize);
    }

    private static string ToBase64Url(string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
