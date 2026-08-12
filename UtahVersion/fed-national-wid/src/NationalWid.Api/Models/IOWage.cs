namespace NationalWid.Api.Models;

/// <summary>
/// WID 3.0 <c>IOWage</c> table — wages by industry and occupation (OES/OEWS).
/// Primary key: StFips, AreaType, AreaTypeVersion, Area, PeriodYear, PeriodType, Period,
/// IndCodeType, IndCode, OccCodeType, OccCode.
/// </summary>
public sealed record IOWage
{
    /// <summary>State FIPS code (2 digits), e.g. '06' for California, '00' for national.</summary>
    public string StFips { get; init; } = "";

    /// <summary>Area type code.</summary>
    public string AreaType { get; init; } = "";

    /// <summary>Area type vintage; default '0' (current definition).</summary>
    public string AreaTypeVersion { get; init; } = "0";

    /// <summary>6-character area code, front-filled with zeroes.</summary>
    public string Area { get; init; } = "";

    /// <summary>Data year, e.g. '2024'.</summary>
    public string PeriodYear { get; init; } = "";

    /// <summary>Period type code; '01' for annual OES.</summary>
    public string PeriodType { get; init; } = "01";

    /// <summary>Period code; '00' for annual.</summary>
    public string Period { get; init; } = "00";

    /// <summary>Occupation code type identifier, e.g. '27' for SOC.</summary>
    public string OccCodeType { get; init; } = "";

    /// <summary>Occupation code, trimmed of hyphens.</summary>
    public string OccCode { get; init; } = "";

    /// <summary>Wage source identifier; '3' = OES/OEWS.</summary>
    public string WageSource { get; init; } = "3";

    /// <summary>Rate type identifier; '2' = hourly.</summary>
    public string RateType { get; init; } = "2";

    /// <summary>Industry code type identifier.</summary>
    public string IndCodeType { get; init; } = "";

    /// <summary>Industry code, trimmed of hyphens. '000000' means cross-industry.</summary>
    public string IndCode { get; init; } = "";

    /// <summary>Total employment for the occupation.</summary>
    public long? EmpCount { get; init; }

    /// <summary>Survey response rate as a percent.</summary>
    public decimal? ResponseRate { get; init; }

    /// <summary>Mean wage for the occupation.</summary>
    public decimal? MeanWage { get; init; }

    /// <summary>Entry-level wage (25th percentile proxy).</summary>
    public decimal? EntryWage { get; init; }

    /// <summary>Experienced-worker wage (75th percentile proxy).</summary>
    public decimal? ExperiencedWage { get; init; }

    /// <summary>10th-percentile wage.</summary>
    public decimal? Percentile10Wage { get; init; }

    /// <summary>25th-percentile wage.</summary>
    public decimal? Percentile25Wage { get; init; }

    /// <summary>Median (50th-percentile) wage.</summary>
    public decimal? MedianWage { get; init; }

    /// <summary>75th-percentile wage.</summary>
    public decimal? Percentile75Wage { get; init; }

    /// <summary>90th-percentile wage.</summary>
    public decimal? Percentile90Wage { get; init; }

    /// <summary>User-defined percentile value.</summary>
    public decimal? UserDefinedPct { get; init; }

    /// <summary>Wage at the user-defined percentile.</summary>
    public decimal? UserDefinedPctWage { get; init; }

    /// <summary>Lower bound of the user-defined wage range percentile.</summary>
    public decimal? UserDefinedRangeLoPct { get; init; }

    /// <summary>Upper bound of the user-defined wage range percentile.</summary>
    public decimal? UserDefinedRangeHiPct { get; init; }

    /// <summary>Mean wage within the user-defined percentile range.</summary>
    public decimal? UserDefinedRangeMean { get; init; }

    /// <summary>Relative percent error for the wage estimate.</summary>
    public decimal? WageRelativePctError { get; init; }

    /// <summary>Relative percent error for the employment estimate.</summary>
    public decimal? EmpRelativePctError { get; init; }

    /// <summary>Panel code for multi-year survey data.</summary>
    public string? PanelCode { get; init; }

    /// <summary>Extension: mean hourly wage (BLS OEWS).</summary>
    public decimal? MeanHourly { get; init; }

    /// <summary>Extension: annual mean wage (BLS OEWS).</summary>
    public decimal? AnnualMean { get; init; }

    /// <summary>Suppress wage values (WID 3.0 SuppressWage, renamed from SuppRecord).</summary>
    public string? SuppressWage { get; init; }

    /// <summary>Suppresses all values for the record.</summary>
    public string? SuppressAll { get; init; }

    /// <summary>Suppresses employment count for the record.</summary>
    public string? SuppressEmp { get; init; }
}
