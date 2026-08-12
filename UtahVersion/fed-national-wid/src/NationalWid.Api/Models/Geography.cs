namespace NationalWid.Api.Models;

/// <summary>
/// WID 3.0 <c>Geographies</c> lookup — geographic area descriptors.
/// Primary key: StFips, AreaType, AreaTypeVersion, Area.
/// </summary>
public sealed record Geography
{
    /// <summary>State FIPS code (2 digits), e.g. '06' for California, '00' for national.</summary>
    public string StFips { get; init; } = "";

    /// <summary>Area type code, e.g. 'SM' for MSA, 'LA' for LAUS area.</summary>
    public string AreaType { get; init; } = "";

    /// <summary>Area type vintage; default '0' (current definition).</summary>
    public string AreaTypeVersion { get; init; } = "0";

    /// <summary>6-character area code, front-filled with zeroes.</summary>
    public string Area { get; init; } = "";

    /// <summary>Geographic area name (WID 3.0 AreaName; column renamed from areatitle).</summary>
    public string? AreaName { get; init; }

    /// <summary>Area type title (extension field, not in WID 3.0 spec).</summary>
    public string? AreaTypeTitle { get; init; }

    /// <summary>Extended geographic description or notes.</summary>
    public string? AreaDesc { get; init; }

    /// <summary>Latitude of the area centroid in decimal degrees.</summary>
    public decimal? Latitude { get; init; }

    /// <summary>Longitude of the area centroid in decimal degrees.</summary>
    public decimal? Longitude { get; init; }

    /// <summary>Code indicating the precision level of the geocoordinates.</summary>
    public string? GeoPrecisionCode { get; init; }
}
