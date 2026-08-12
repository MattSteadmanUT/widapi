namespace NationalWid.Ingestion.Models;

// Upsert row shapes mirroring the WID 3.0 tables (see cloud-deployment/migrations).
// All key fields are strings per the WID char-code convention; flags are '0'/'1'.

public sealed record LaborForceRow
{
    public string StFips { get; init; } = "00";
    public string AreaType { get; init; } = "00";
    public string AreaTypeVersion { get; init; } = "0";
    public string Area { get; init; } = "000000";
    public required string PeriodYear { get; init; }
    public required string PeriodType { get; init; }
    public required string Period { get; init; }
    public string Adjusted { get; init; } = "1";
    public long? LaborForce { get; set; }
    public long? Employed { get; set; }
    public long? Unemployed { get; set; }
    public decimal? UnempRate { get; set; }
    public string SuppRecord { get; init; } = "0";
    public string SuppRate { get; init; } = "0";
    public string Prelim { get; set; } = "0";
}

public sealed record CesRow
{
    public string StFips { get; init; } = "00";
    public string AreaType { get; init; } = "00";
    public string AreaTypeVersion { get; init; } = "0";
    public string Area { get; init; } = "000000";
    public required string PeriodYear { get; init; }
    public required string PeriodType { get; init; }
    public required string Period { get; init; }
    public string Adjusted { get; init; } = "1";
    public required string SeriesCodeType { get; init; }
    public required string SeriesCode { get; init; }
    public long? EmpCes { get; set; }
    public long? EmpProductionWorkers { get; set; }
    public decimal? HoursPerWeek { get; set; }
    public decimal? EarningsPerWeek { get; set; }
    public decimal? EarningsPerHour { get; set; }
    public decimal? AvgWeeklyEarningsPctChange { get; set; }
    public string SuppRecord { get; init; } = "0";
    public string SuppHoursEarnings { get; init; } = "0";
    public string SuppProdWorkers { get; init; } = "0";
    public string Prelim { get; set; } = "0";
}

public sealed record IndustryRow
{
    public string StFips { get; init; } = "00";
    public string AreaType { get; init; } = "00";
    public string AreaTypeVersion { get; init; } = "0";
    public string Area { get; init; } = "000000";
    public required string PeriodYear { get; init; }
    public required string PeriodType { get; init; }
    public required string Period { get; init; }
    public required string Ownership { get; init; }
    public string IndCodeType { get; init; } = "10";
    public required string IndCode { get; init; }
    public decimal? AvgMonthlyEmp { get; set; }
    public decimal? TotalWages { get; set; }
    public decimal? TaxableWages { get; set; }
    public decimal? Contributions { get; set; }
    public decimal? WeeklyWage { get; set; }
    public long? EmpCount { get; set; }
    public string? HighEmpQ { get; set; }
    public string? LowEmpQ { get; set; }
    public string SuppRecord { get; set; } = "0";
    public string Prelim { get; set; } = "0";
}

public sealed record IOWageRow
{
    public string StFips { get; init; } = "00";
    public string AreaType { get; init; } = "00";
    public string AreaTypeVersion { get; init; } = "0";
    public string Area { get; init; } = "000000";
    public required string PeriodYear { get; init; }
    public string PeriodType { get; init; } = "01";
    public string Period { get; init; } = "00";
    public string OccCodeType { get; init; } = "19";
    public required string OccCode { get; init; }
    public string IndCodeType { get; init; } = "10";
    public string IndCode { get; init; } = "000000";
    public string WageSource { get; init; } = "3";
    public string RateType { get; init; } = "2";
    public long? EmpCount { get; set; }
    public decimal? Pct10 { get; set; }
    public decimal? Pct25 { get; set; }
    public decimal? MedianWage { get; set; }
    public decimal? MeanWage { get; set; }
    public decimal? Pct75 { get; set; }
    public decimal? Pct90 { get; set; }
    public decimal? MeanHourly { get; set; }
    public decimal? AnnualMean { get; set; }
    public string SuppRecord { get; set; } = "0";
}

public sealed record ProjectionsMatrixRow
{
    public string StFips { get; init; } = "00";
    public string AreaType { get; init; } = "00";
    public string AreaTypeVersion { get; init; } = "0";
    public string Area { get; init; } = "000000";
    public required string ProjectionsPeriod { get; init; }
    public string IndCodeType { get; init; } = "10";
    public string IndCode { get; init; } = "000000";
    public string OccCodeType { get; init; } = "19";
    public required string OccCode { get; init; }
    public decimal? BaseYearEmp { get; set; }
    public decimal? ProjectedEmp { get; set; }
    public decimal? Change { get; set; }
    public decimal? PctChange { get; set; }
    public decimal? Openings { get; set; }
    public string SuppRecord { get; init; } = "0";
}

public sealed record CesCodeRow
{
    public string StFips { get; init; } = "00";
    public required string SeriesCodeType { get; init; }
    public required string SeriesCode { get; init; }
    public string? SeriesTitle { get; init; }
    public string? IndCode { get; init; }
    public string? IndCodeType { get; init; }
}



