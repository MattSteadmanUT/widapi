namespace NationalWid.Api.Models;

/// <summary>
/// WID 3.0 <c>CESCodes</c> lookup — Current Employment Statistics series codes.
/// Primary key: StFips, SeriesCodeType, SeriesCode. (StFips added in the 5/1/2025 addendum.)
/// </summary>
public sealed record CesCode
{
    /// <summary>State FIPS code (2 digits). Added in the 5/1/2025 WID addendum.</summary>
    public string StFips { get; init; } = "";

    /// <summary>CES series code type identifier.</summary>
    public string SeriesCodeType { get; init; } = "";

    /// <summary>CES industry or supersector series code.</summary>
    public string SeriesCode { get; init; } = "";

    /// <summary>Human-readable title for the series code.</summary>
    public string? SeriesTitle { get; init; }

    /// <summary>Industry code associated with this series code (extension field).</summary>
    public string? IndCode { get; init; }

    /// <summary>Industry code type for <see cref="IndCode"/> (extension field).</summary>
    public string? IndCodeType { get; init; }
}
