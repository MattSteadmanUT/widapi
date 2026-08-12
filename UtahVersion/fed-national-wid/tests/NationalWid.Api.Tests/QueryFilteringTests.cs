using NationalWid.Api.Models;
using NationalWid.Api.Services;

namespace NationalWid.Api.Tests;

public sealed class QueryFilteringTests
{
    [Fact]
    public void SplitCsv_ReturnsTrimmedTokens()
    {
        var values = QueryFiltering.SplitCsv(" 11-1011 , 11-1021, ,11-1031 ");

        Assert.Equal(3, values.Count);
        Assert.Equal("11-1011", values[0]);
        Assert.Equal("11-1021", values[1]);
        Assert.Equal("11-1031", values[2]);
    }

    [Fact]
    public void ApplyStringFilter_SupportsCommaSeparatedExactValues()
    {
        var rows = new[]
        {
            new IOWage { OccCode = "111011" },
            new IOWage { OccCode = "111021" },
            new IOWage { OccCode = "151252" },
        }.AsQueryable();

        var filtered = QueryFiltering.ApplyStringFilter(
                rows,
                nameof(IOWage.OccCode),
                "11-1011,11-1021",
                WidCodes.NormalizeCodePattern)
            .ToList();

        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, row => Assert.StartsWith("11", row.OccCode, StringComparison.Ordinal));
    }

    [Fact]
    public void ApplyStringFilter_SupportsWildcardPatterns()
    {
        var rows = new[]
        {
            new IOWage { OccCode = "111011" },
            new IOWage { OccCode = "111099" },
            new IOWage { OccCode = "151252" },
        }.AsQueryable();

        var filtered = QueryFiltering.ApplyStringFilter(
                rows,
                nameof(IOWage.OccCode),
                "11*",
                WidCodes.NormalizeCodePattern,
                allowWildcard: true)
            .ToList();

        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, row => Assert.StartsWith("11", row.OccCode, StringComparison.Ordinal));
    }
}
