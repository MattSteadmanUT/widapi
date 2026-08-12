namespace NationalWid.Api.Models;

/// <summary>
/// WID 3.0 <c>IndDirectories</c> lookup — industry codes for which projections are performed.
/// Primary key: StFips, ProjPeriod, IndCodeType, IndCode.
/// </summary>
public sealed record IndDirectory
{
    /// <summary>State FIPS code (2 digits).</summary>
    public string StFips { get; init; } = "";

    /// <summary>Projection span, e.g. '2022-2032'.</summary>
    public string ProjPeriod { get; init; } = "";

    /// <summary>Industry code type identifier.</summary>
    public string IndCodeType { get; init; } = "";

    /// <summary>Industry code, trimmed of hyphens.</summary>
    public string IndCode { get; init; } = "";

    /// <summary>Industry title.</summary>
    public string? IndTitle { get; init; }
}
