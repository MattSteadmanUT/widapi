namespace NationalWid.Api.Models;

/// <summary>
/// WID 3.0 <c>CES</c> table — Current Employment Statistics estimates.
/// Primary key: StFips, AreaType, AreaTypeVersion, Area, PeriodYear, PeriodType, Period,
/// SeriesCodeType, SeriesCode, Adjusted.
/// </summary>
public sealed record Ces
{
    /// <summary>State FIPS code (2 digits), e.g. '06' for California, '00' for national.</summary>
    public string StFips { get; init; } = "";

    /// <summary>Area type code, e.g. 'SM' for metropolitan statistical area.</summary>
    public string AreaType { get; init; } = "";

    /// <summary>Area type vintage; default '0' (current definition).</summary>
    public string AreaTypeVersion { get; init; } = "0";

    /// <summary>6-character area code, front-filled with zeroes.</summary>
    public string Area { get; init; } = "";

    /// <summary>Data year, e.g. '2024'.</summary>
    public string PeriodYear { get; init; } = "";

    /// <summary>Period type code, e.g. 'SM' for monthly.</summary>
    public string PeriodType { get; init; } = "";

    /// <summary>Period code; '00' where the period type is annual.</summary>
    public string Period { get; init; } = "00";

    /// <summary>'1' = seasonally adjusted, '0' = not adjusted.</summary>
    public string Adjusted { get; init; } = "0";

    /// <summary>CES series code type identifier.</summary>
    public string SeriesCodeType { get; init; } = "";

    /// <summary>CES industry or supersector series code.</summary>
    public string SeriesCode { get; init; } = "";

    /// <summary>Employment by place of work; actual count, not in thousands.</summary>
    public long? EmpCES { get; init; }

    /// <summary>Production and non-supervisory workers employment count.</summary>
    public long? EmpProductionWorkers { get; init; }

    /// <summary>Average weekly hours for production workers.</summary>
    public decimal? HoursPerWeek { get; init; }

    /// <summary>Average weekly earnings for production workers.</summary>
    public decimal? EarningsPerWeek { get; init; }

    /// <summary>Average hourly earnings for production workers.</summary>
    public decimal? EarningsPerHour { get; init; }

    /// <summary>Year-over-year percent change in average weekly earnings.</summary>
    public decimal? AvgWeeklyEarningsPctChange { get; init; }

    /// <summary>Record-level suppression flag.</summary>
    public string? SuppRecord { get; init; }

    /// <summary>Suppression flag for hours and earnings fields.</summary>
    public string? SuppHoursEarnings { get; init; }

    /// <summary>Suppression flag for production workers fields.</summary>
    public string? SuppProdWorkers { get; init; }

    /// <summary>Preliminary data indicator ('1' if preliminary).</summary>
    public string? Prelim { get; init; }
}
