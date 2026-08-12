using NationalWid.Api.Models;
using NationalWid.Api.Services;
using Microsoft.AspNetCore.Http;

namespace NationalWid.Api.Tests;

public sealed class QuerySortingTests
{
    [Fact]
    public void Apply_UsesDefaultOrderingWhenSortMissing()
    {
        var rows = new[]
        {
            new LaborForce { StFips = "49", PeriodYear = "2023" },
            new LaborForce { StFips = "00", PeriodYear = "2024" },
        }.AsQueryable();

        var ordered = QuerySorting.Apply(rows, null, nameof(LaborForce.StFips), nameof(LaborForce.PeriodYear)).ToList();

        Assert.Equal("00", ordered[0].StFips);
        Assert.Equal("49", ordered[1].StFips);
    }

    [Fact]
    public void Apply_SupportsMultiFieldDirections()
    {
        var rows = new[]
        {
            new LaborForce { StFips = "00", PeriodYear = "2024", Period = "01" },
            new LaborForce { StFips = "00", PeriodYear = "2024", Period = "12" },
            new LaborForce { StFips = "00", PeriodYear = "2023", Period = "12" },
        }.AsQueryable();

        var ordered = QuerySorting.Apply(rows, "periodYear:desc,period:asc", nameof(LaborForce.StFips)).ToList();

        Assert.Equal("2024", ordered[0].PeriodYear);
        Assert.Equal("01", ordered[0].Period);
        Assert.Equal("2024", ordered[1].PeriodYear);
        Assert.Equal("12", ordered[1].Period);
        Assert.Equal("2023", ordered[2].PeriodYear);
    }

    [Fact]
    public async Task Apply_ThrowsForUnknownSortField()
    {
        var rows = new[] { new LaborForce { StFips = "00" } }.AsQueryable();

        var ex = await Assert.ThrowsAsync<BadHttpRequestException>(() =>
            Task.Run(() => QuerySorting.Apply(rows, "doesNotExist:asc", nameof(LaborForce.StFips)).ToList()));

        Assert.Contains("Unknown sort field", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Apply_AcceptsHumanReadableFieldLabels()
    {
        var rows = new[]
        {
            new Industry { AvgMonthlyEmp = 120m, IndCode = "31" },
            new Industry { AvgMonthlyEmp = 50m, IndCode = "32" },
            new Industry { AvgMonthlyEmp = 900m, IndCode = "33" },
        }.AsQueryable();

        var ordered = QuerySorting.Apply(rows, "Avg Monthly Emp:desc", nameof(Industry.IndCode)).ToList();

        Assert.Equal(900m, ordered[0].AvgMonthlyEmp);
        Assert.Equal(120m, ordered[1].AvgMonthlyEmp);
        Assert.Equal(50m, ordered[2].AvgMonthlyEmp);
    }
}
