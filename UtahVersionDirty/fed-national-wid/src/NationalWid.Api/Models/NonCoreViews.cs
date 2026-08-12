namespace NationalWid.Api.Models;

public sealed record CesWithGeography
{
    public string StFips { get; init; } = "";
    public string AreaType { get; init; } = "";
    public string AreaTypeVersion { get; init; } = "0";
    public string Area { get; init; } = "";
    public string PeriodYear { get; init; } = "";
    public string PeriodType { get; init; } = "";
    public string Period { get; init; } = "00";
    public string Adjusted { get; init; } = "0";
    public string SeriesCodeType { get; init; } = "";
    public string SeriesCode { get; init; } = "";
    public long? EmpCES { get; init; }
    public long? EmpProductionWorkers { get; init; }
    public decimal? HoursPerWeek { get; init; }
    public decimal? EarningsPerWeek { get; init; }
    public decimal? EarningsPerHour { get; init; }
    public decimal? AvgWeeklyEarningsPctChange { get; init; }
    public string? SuppRecord { get; init; }
    public string? SuppHoursEarnings { get; init; }
    public string? SuppProdWorkers { get; init; }
    public string? Prelim { get; init; }
    public string? AreaName { get; init; }
    public string? SeriesTitle { get; init; }
}

public sealed record LaborForceWithGeography
{
    public string StFips { get; init; } = "";
    public string AreaType { get; init; } = "";
    public string AreaTypeVersion { get; init; } = "0";
    public string Area { get; init; } = "";
    public string PeriodYear { get; init; } = "";
    public string PeriodType { get; init; } = "";
    public string Period { get; init; } = "00";
    public string Adjusted { get; init; } = "0";
    public long? CivilianLaborForce { get; init; }
    public long? Employed { get; init; }
    public long? Unemployed { get; init; }
    public decimal? UnempRate { get; init; }
    public string? SuppRecord { get; init; }
    public string? SuppRate { get; init; }
    public string? Prelim { get; init; }
    public string? AreaName { get; init; }
}

public sealed record IndustryWithGeography
{
    public string StFips { get; init; } = "";
    public string AreaType { get; init; } = "";
    public string AreaTypeVersion { get; init; } = "0";
    public string Area { get; init; } = "";
    public string PeriodYear { get; init; } = "";
    public string PeriodType { get; init; } = "";
    public string Period { get; init; } = "00";
    public string Ownership { get; init; } = "";
    public string IndCodeType { get; init; } = "";
    public string IndCode { get; init; } = "";
    public decimal? AvgMonthlyEmp { get; init; }
    public decimal? TotalWages { get; init; }
    public decimal? TaxableWages { get; init; }
    public decimal? UIContributions { get; init; }
    public decimal? WeeklyWage { get; init; }
    public long? EmpCount { get; init; }
    public string? HighEmpQ { get; init; }
    public string? LowEmpQ { get; init; }
    public string? SuppRecord { get; init; }
    public string? Prelim { get; init; }
    public string? AreaName { get; init; }
    public string? IndustryTitle { get; init; }
    public string? OwnershipTitle { get; init; }
}

public sealed record WageWithDescriptions
{
    public string StFips { get; init; } = "";
    public string AreaType { get; init; } = "";
    public string AreaTypeVersion { get; init; } = "0";
    public string Area { get; init; } = "";
    public string PeriodYear { get; init; } = "";
    public string PeriodType { get; init; } = "01";
    public string Period { get; init; } = "00";
    public string OccCodeType { get; init; } = "";
    public string OccCode { get; init; } = "";
    public string IndCodeType { get; init; } = "";
    public string IndCode { get; init; } = "";
    public string WageSource { get; init; } = "3";
    public string RateType { get; init; } = "2";
    public long? EmpCount { get; init; }
    public decimal? Percentile10Wage { get; init; }
    public decimal? Percentile25Wage { get; init; }
    public decimal? MedianWage { get; init; }
    public decimal? MeanWage { get; init; }
    public decimal? Percentile75Wage { get; init; }
    public decimal? Percentile90Wage { get; init; }
    public decimal? MeanHourly { get; init; }
    public decimal? AnnualMean { get; init; }
    public string? SuppressWage { get; init; }
    public string? AreaName { get; init; }
    public string? OccupationTitle { get; init; }
    public string? IndustryTitle { get; init; }
    public string? WageSourceTitle { get; init; }
    public string? RateTypeTitle { get; init; }
}

public sealed record ProjectionWithTitles
{
    public string StFips { get; init; } = "";
    public string AreaType { get; init; } = "";
    public string AreaTypeVersion { get; init; } = "0";
    public string Area { get; init; } = "";
    public string ProjectionsPeriod { get; init; } = "";
    public string IndCodeType { get; init; } = "";
    public string IndCode { get; init; } = "";
    public string OccCodeType { get; init; } = "";
    public string OccCode { get; init; } = "";
    public long? BaseYearEmp { get; init; }
    public long? ProjectedEmp { get; init; }
    public long? Change { get; init; }
    public decimal? PctChange { get; init; }
    public long? Openings { get; init; }
    public string? SuppRecord { get; init; }
    public string? AreaName { get; init; }
    public string? IndustryTitle { get; init; }
    public string? OccupationTitle { get; init; }
    public string? GrowthCodeTitle { get; init; }
}

public sealed record LicensingByOccupation
{
    public string StFips { get; init; } = "";
    public string OccCodeType { get; init; } = "";
    public string OccCode { get; init; } = "";
    public string? OccupationTitle { get; init; }
    public string LicenseID { get; init; } = "";
    public string? LicenseTitle { get; init; }
    public string? LicenseType { get; init; }
    public string? LicAuthID { get; init; }
    public string? Board { get; init; }
    public string? LicenseURL { get; init; }
    public DateOnly? LicenseUpdatedDate { get; init; }
}
