using NationalWid.Api.Models;

namespace NationalWid.Api.Tests;

public sealed class LicenseModelTests
{
    [Fact]
    public void LicenseUpdatedDate_ParsesEightDigitSourceStamp()
    {
        var license = new License
        {
            LicenseUpdated = "20240819"
        };

        Assert.Equal(new DateOnly(2024, 8, 19), license.LicenseUpdatedDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-date")]
    [InlineData("2024-08-19")]
    public void LicenseUpdatedDate_ReturnsNull_WhenStampCannotBeParsed(string? stamp)
    {
        var license = new License
        {
            LicenseUpdated = stamp
        };

        Assert.Null(license.LicenseUpdatedDate);
    }
}
