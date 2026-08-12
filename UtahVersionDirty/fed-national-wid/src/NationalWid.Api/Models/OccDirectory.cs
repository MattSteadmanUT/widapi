namespace NationalWid.Api.Models;

/// <summary>
/// WID 3.0 <c>OccDirectories</c> lookup — occupation codes for which projections are performed.
/// Primary key: StFips, ProjPeriod, OccCodeType, OccCode.
/// </summary>
public sealed record OccDirectory
{
    /// <summary>State FIPS code (2 digits).</summary>
    public string StFips { get; init; } = "";

    /// <summary>Projection span, e.g. '2022-2032'.</summary>
    public string ProjPeriod { get; init; } = "";

    /// <summary>Occupation code type identifier.</summary>
    public string OccCodeType { get; init; } = "";

    /// <summary>Occupation code, trimmed of hyphens.</summary>
    public string OccCode { get; init; } = "";

    /// <summary>Occupation title.</summary>
    public string? OccTitle { get; init; }
}
