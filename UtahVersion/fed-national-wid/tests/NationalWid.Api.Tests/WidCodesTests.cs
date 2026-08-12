using NationalWid.Api.Services;

namespace NationalWid.Api.Tests;

public sealed class WidCodesTests
{
    [Theory]
    [InlineData("49", "49")]
    [InlineData(" 00 ", "00")]
    [InlineData(null, null)]
    [InlineData("", null)]
    public void NormalizeStFips_TrimsAndNulls(string? input, string? expected)
    {
        Assert.Equal(expected, WidCodes.NormalizeStFips(input));
    }

    [Theory]
    [InlineData("00", true)]
    [InlineData("49", true)]
    [InlineData("9", false)]
    [InlineData("490", false)]
    [InlineData("AA", false)]
    public void IsValidStFips_ValidatesTwoDigitNumeric(string input, bool expected)
    {
        Assert.Equal(expected, WidCodes.IsValidStFips(input));
    }
}
