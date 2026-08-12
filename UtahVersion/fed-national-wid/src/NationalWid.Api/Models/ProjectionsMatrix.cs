namespace NationalWid.Api.Models;

/// <summary>
/// WID 3.0 <c>ProjectionsMatrix</c> table — industry/occupation employment projections.
/// Primary key: StFips, AreaType, AreaTypeVersion, Area, ProjectionsPeriod,
/// IndCodeType, IndCode, OccCodeType, OccCode.
/// </summary>
public sealed record ProjectionsMatrix
{
    /// <summary>State FIPS code (2 digits), e.g. '06' for California, '00' for national.</summary>
    public string StFips { get; init; } = "";

    /// <summary>Area type code.</summary>
    public string AreaType { get; init; } = "";

    /// <summary>Area type vintage; default '0' (current definition).</summary>
    public string AreaTypeVersion { get; init; } = "0";

    /// <summary>6-character area code, front-filled with zeroes.</summary>
    public string Area { get; init; } = "";

    /// <summary>Projection span, e.g. '2022-2032'.</summary>
    public string ProjectionsPeriod { get; init; } = "";

    /// <summary>Industry code type identifier.</summary>
    public string IndCodeType { get; init; } = "";

    /// <summary>Industry code, trimmed of hyphens.</summary>
    public string IndCode { get; init; } = "";

    /// <summary>Occupation code type identifier.</summary>
    public string OccCodeType { get; init; } = "";

    /// <summary>Occupation code, trimmed of hyphens.</summary>
    public string OccCode { get; init; } = "";

    /// <summary>Base-year employment level.</summary>
    public long? BaseYearEmp { get; init; }

    /// <summary>Projected employment level at the end of the projection span.</summary>
    public long? ProjectedEmp { get; init; }

    /// <summary>Percent of base-year industry employment for this occupation.</summary>
    public decimal? PctEstInd { get; init; }

    /// <summary>Percent of base-year occupation employment in this industry.</summary>
    public decimal? PctEstOcc { get; init; }

    /// <summary>Percent of projected industry employment for this occupation.</summary>
    public decimal? PctProjInd { get; init; }

    /// <summary>Percent of projected occupation employment in this industry.</summary>
    public decimal? PctProjOcc { get; init; }

    /// <summary>Numeric change from base-year to projected employment.</summary>
    public long? Change { get; init; }

    /// <summary>Percent change from base-year to projected employment.</summary>
    public decimal? PctChange { get; init; }

    /// <summary>Growth code indicating relative growth category.</summary>
    public string? GrowthCode { get; init; }

    /// <summary>Estimated occupational exits (retirements and other separations) over the span.</summary>
    public long? Exits { get; init; }

    /// <summary>Annual average exits over the projection span.</summary>
    public long? AnnualExits { get; init; }

    /// <summary>Estimated occupational transfers over the projection span.</summary>
    public long? Transfers { get; init; }

    /// <summary>Annual average transfers over the projection span.</summary>
    public long? AnnualTransfers { get; init; }

    /// <summary>Annual average numeric change in employment.</summary>
    public long? AnnualChange { get; init; }

    /// <summary>Total openings = exits + transfers + change.</summary>
    public long? Openings { get; init; }

    /// <summary>Annual average total job openings over the projection span.</summary>
    public long? AnnualOpenings { get; init; }

    /// <summary>Suppression flag for the record.</summary>
    public string? Suppress { get; init; }
}
