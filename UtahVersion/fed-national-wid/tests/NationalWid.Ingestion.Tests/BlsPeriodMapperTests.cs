using NationalWid.Ingestion.Services;

namespace NationalWid.Ingestion.Tests;

public class BlsPeriodMapperTests
{
    [Theory]
    [InlineData("M01", "03", "01")]
    [InlineData("M12", "03", "12")]
    [InlineData("m03", "03", "03")]
    [InlineData("M13", "01", "00")]
    [InlineData("Q01", "02", "01")]
    [InlineData("Q04", "02", "04")]
    [InlineData("q02", "02", "02")]
    [InlineData("Q05", "01", "00")]
    [InlineData("A01", "01", "00")]
    [InlineData("A99", "01", "00")]
    public void TryMap_ReturnsExpectedMappedValues(string blsPeriod, string expectedPeriodType, string expectedPeriod)
    {
        var mapped = BlsPeriodMapper.TryMap(blsPeriod, out var periodType, out var period);

        Assert.True(mapped);
        Assert.Equal(expectedPeriodType, periodType);
        Assert.Equal(expectedPeriod, period);
    }

    [Theory]
    [InlineData("H01")]
    [InlineData("H02")]
    [InlineData("h01")]
    public void TryMap_ReturnsFalse_ForHalfYearPeriods(string blsPeriod)
    {
        var mapped = BlsPeriodMapper.TryMap(blsPeriod, out var periodType, out var period);

        Assert.False(mapped);
        Assert.Equal(string.Empty, periodType);
        Assert.Equal(string.Empty, period);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("M")]
    [InlineData("M0")]
    [InlineData("Q")]
    [InlineData("A")]
    [InlineData("M00")]
    [InlineData("M14")]
    [InlineData("MAB")]
    [InlineData("Q00")]
    [InlineData("Q06")]
    [InlineData("QAB")]
    [InlineData("Z01")]
    public void TryMap_ReturnsFalse_ForInvalidOrOutOfRangePeriods(string? blsPeriod)
    {
        var mapped = BlsPeriodMapper.TryMap(blsPeriod!, out var periodType, out var period);

        Assert.False(mapped);
        Assert.Equal(string.Empty, periodType);
        Assert.Equal(string.Empty, period);
    }
}
