using System.Text.Json.Serialization;

namespace NationalWid.Api.Models;

/// <summary>
/// WID 3.0 <c>LaborForce</c> table — LAUS employment and unemployment estimates.
/// Primary key: StFips, AreaType, AreaTypeVersion, Area, PeriodYear, PeriodType, Period, Adjusted.
/// Flag fields are char(1) strings ('0'/'1') per WID 3.0 convention, never booleans.
/// </summary>
public sealed record LaborForce
{
    /// <summary>State FIPS code (2 digits), e.g. '06' for California, '00' for national.</summary>
    public string StFips { get; init; } = "";

    /// <summary>Area type code, e.g. 'LA' for LAUS area.</summary>
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

    /// <summary>'1' = seasonally adjusted, '0' = not adjusted.</summary>
    public string Adjusted { get; init; } = "0";

    /// <summary>Civilian labor force. (Property renamed from the WID field to avoid clashing with the type name.)</summary>
    [JsonPropertyName("laborForce")]
    public long? CivilianLaborForce { get; init; }

    /// <summary>Number of employed persons.</summary>
    public long? Employed { get; init; }

    /// <summary>Number of unemployed persons.</summary>
    public long? Unemployed { get; init; }

    /// <summary>Unemployment rate as a percent.</summary>
    public decimal? UnempRate { get; init; }

    /// <summary>Benchmark year indicator.</summary>
    public string? Benchmark { get; init; }

    /// <summary>Civilian labor force participation rate as a percent.</summary>
    public decimal? CLFPRate { get; init; }

    /// <summary>Employment-to-population ratio as a percent.</summary>
    public decimal? EmpPopRatio { get; init; }

    /// <summary>Record-level suppression flag.</summary>
    public string? SuppRecord { get; init; }

    /// <summary>Suppression flag for the unemployment rate field.</summary>
    public string? SuppRate { get; init; }

    /// <summary>Preliminary data indicator ('1' if preliminary).</summary>
    public string? Prelim { get; init; }
}
