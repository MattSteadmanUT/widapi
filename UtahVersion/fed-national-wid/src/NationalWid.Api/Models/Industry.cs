namespace NationalWid.Api.Models;

/// <summary>
/// WID 3.0 <c>Industry</c> table — QCEW (ES-202) covered employment and wages by industry.
/// Primary key: StFips, AreaType, AreaTypeVersion, Area, PeriodYear, PeriodType, Period,
/// Ownership, CodeType, IndCode.
/// </summary>
public sealed record Industry
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

    /// <summary>Period type code.</summary>
    public string PeriodType { get; init; } = "";

    /// <summary>Period code; '00' where the period type is annual.</summary>
    public string Period { get; init; } = "00";

    /// <summary>Public/private ownership indicator (2 digits).</summary>
    public string Ownership { get; init; } = "";

    /// <summary>Industry code type, e.g. '10' = NAICS (renamed from CodeType to match WID 3.0 spec).</summary>
    public string IndCodeType { get; init; } = "";

    /// <summary>Industry code (e.g. NAICS code, trimmed of hyphens).</summary>
    public string IndCode { get; init; } = "";

    /// <summary>Number of firms in the industry.</summary>
    public decimal? Firms { get; init; }

    /// <summary>Number of employer establishments (reporting units).</summary>
    public decimal? Establishments { get; init; }

    /// <summary>Quarter average employment (WID 3.0 QuarterAvgEmp).</summary>
    public decimal? QuarterAvgEmp { get; init; }

    /// <summary>Month 1 employment.</summary>
    public decimal? Month1Emp { get; init; }

    /// <summary>Month 2 employment.</summary>
    public decimal? Month2Emp { get; init; }

    /// <summary>Month 3 employment.</summary>
    public decimal? Month3Emp { get; init; }

    /// <summary>Top employer average employment.</summary>
    public decimal? TopEmployerAvgEmp { get; init; }

    /// <summary>Total wages paid during the period.</summary>
    public decimal? TotalWages { get; init; }

    /// <summary>Taxable wages for UI purposes.</summary>
    public decimal? TaxableWages { get; init; }

    /// <summary>Employer contributions to the UI fund (WID 3.0 UIContributions).</summary>
    public decimal? UIContributions { get; init; }

    /// <summary>Average weekly wage.</summary>
    public decimal? WeeklyWage { get; init; }

    /// <summary>Extension: BLS QCEW avg monthly employment (kept for backward compat).</summary>
    public decimal? AvgMonthlyEmp { get; init; }

    /// <summary>Extension: QCEW establishment count (kept for backward compat).</summary>
    public long? EmpCount { get; init; }

    /// <summary>Quarter with the highest employment level.</summary>
    public string? HighEmpQ { get; init; }

    /// <summary>Quarter with the lowest employment level.</summary>
    public string? LowEmpQ { get; init; }

    /// <summary>Suppress flag (WID 3.0 Suppress field).</summary>
    public string? SuppRecord { get; init; }

    /// <summary>Preliminary data indicator ('1' if preliminary).</summary>
    public string? Prelim { get; init; }
}
