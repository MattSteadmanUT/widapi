using System.Text.Json.Serialization;

namespace NationalWid.Api.Models;

/// <summary>
/// WID 3.0 <c>PeriodYears</c> lookup — valid data years per state and period type.
/// Primary key: StFips, PeriodYear, PeriodType.
/// </summary>
public sealed record PeriodYear
{
    /// <summary>State FIPS code (2 digits).</summary>
    public string StFips { get; init; } = "";

    /// <summary>The data year, e.g. '2024'. (Property renamed to avoid clashing with the type name.)</summary>
    [JsonPropertyName("periodYear")]
    public string Year { get; init; } = "";

    /// <summary>Period type code, e.g. 'SM' for monthly, '01' for annual.</summary>
    public string PeriodType { get; init; } = "";

    /// <summary>Period code; '00' for annual periods.</summary>
    public string Period { get; init; } = "00";

    /// <summary>Human-readable period title.</summary>
    public string? PeriodTitle { get; init; }
}
