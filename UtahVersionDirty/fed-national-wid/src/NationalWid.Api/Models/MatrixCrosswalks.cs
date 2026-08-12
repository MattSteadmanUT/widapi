namespace NationalWid.Api.Models;

/// <summary>
/// WID 3.0 <c>MatrixXInd</c> crosswalk table — maps projections-matrix industry codes to their
/// underlying detailed industry codes.
/// Primary key: StFips, MatrixIndCode, IndCodeType, IndCode.
/// </summary>
public sealed record MatrixXInd
{
    /// <summary>State FIPS code (2 digits).</summary>
    public string StFips { get; init; } = "";

    /// <summary>Aggregated matrix industry code used in the projections matrix.</summary>
    public string MatrixIndCode { get; init; } = "";

    /// <summary>Detailed industry code type identifier.</summary>
    public string IndCodeType { get; init; } = "";

    /// <summary>Detailed industry code that rolls up into <see cref="MatrixIndCode"/>.</summary>
    public string IndCode { get; init; } = "";
}

/// <summary>
/// WID 3.0 <c>MatrixXOcc</c> crosswalk table — maps projections-matrix occupation codes to their
/// underlying detailed occupation codes.
/// Primary key: StFips, MatrixOccCode, OccCodeType, OccCode.
/// </summary>
public sealed record MatrixXOcc
{
    /// <summary>State FIPS code (2 digits).</summary>
    public string StFips { get; init; } = "";

    /// <summary>Aggregated matrix occupation code used in the projections matrix.</summary>
    public string MatrixOccCode { get; init; } = "";

    /// <summary>Detailed occupation code type identifier.</summary>
    public string OccCodeType { get; init; } = "";

    /// <summary>Detailed occupation code that rolls up into <see cref="MatrixOccCode"/>.</summary>
    public string OccCode { get; init; } = "";
}
