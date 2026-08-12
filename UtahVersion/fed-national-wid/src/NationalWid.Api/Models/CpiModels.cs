namespace NationalWid.Api.Models;

// =============================================================================
// Consumer Price Index (CPI) model classes.
// Data source: BLS Time Series database
//   https://download.bls.gov/pub/time.series/cu/
// =============================================================================

/// <summary>
/// A BLS CPI time series observation: the CPI value for a specific series, year, and period.
/// The series identifies the geographic area, item basket, seasonality, and periodicity.
/// </summary>
public sealed record Cpi
{
    /// <summary>
    /// BLS time series ID (e.g., <c>CUSR0000SA0</c>).
    /// Format: CU + [S|U] + [A|M|S] + areaCode + itemCode.
    /// </summary>
    public string SeriesId { get; init; } = "";

    /// <summary>Four-digit calendar year (e.g., 2023).</summary>
    public short Year { get; init; }

    /// <summary>
    /// BLS period code (e.g., <c>M01</c>–<c>M12</c> for January–December,
    /// <c>M13</c> for annual average, <c>S01</c>/<c>S02</c> for semi-annual).
    /// </summary>
    public string Period { get; init; } = "";

    /// <summary>CPI index value; null when the BLS record is suppressed.</summary>
    public decimal? Value { get; init; }

    /// <summary>BLS footnote codes attached to this observation, if any.</summary>
    public string? Footnotes { get; init; }
}

/// <summary>
/// BLS CPI series descriptor: joins area, item, periodicity, and seasonal adjustment
/// codes into a single searchable index row.
/// </summary>
public sealed record CpiSeries
{
    /// <summary>Full BLS series ID (e.g., <c>CUSR0000SA0</c>).</summary>
    public string SeriesId { get; init; } = "";

    /// <summary>Seasonal adjustment code: <c>S</c> = seasonally adjusted, <c>U</c> = not adjusted.</summary>
    public string SeasonalCode { get; init; } = "";

    /// <summary>Periodicity code: <c>M</c> = monthly, <c>S</c> = semi-annual, <c>A</c> = annual.</summary>
    public string PeriodicityCode { get; init; } = "";

    /// <summary>BLS area code (e.g., <c>0000</c> for U.S. city average).</summary>
    public string AreaCode { get; init; } = "";

    /// <summary>BLS item code identifying the measured price basket (e.g., <c>SA0</c> for All items).</summary>
    public string ItemCode { get; init; } = "";

    /// <summary>Base period type code (e.g., <c>BURS</c>).</summary>
    public string? BaseTypeCode { get; init; }

    /// <summary>Base year string as provided by BLS (e.g., <c>1982-84=100</c>).</summary>
    public string? BaseYear { get; init; }

    /// <summary>Raw footnote codes string from the BLS source file.</summary>
    public string? FootnoteCodesStr { get; init; }

    /// <summary>Earliest period code in the series.</summary>
    public string? BeginPeriod { get; init; }

    /// <summary>Earliest year in the series.</summary>
    public short? BeginYear { get; init; }

    /// <summary>Latest period code in the series.</summary>
    public string? EndPeriod { get; init; }

    /// <summary>Latest year in the series.</summary>
    public short? EndYear { get; init; }

    /// <summary>Human-readable series name derived from component labels.</summary>
    public string? SeriesName { get; init; }
}

/// <summary>BLS CPI item code reference record (e.g., SA0 = All items).</summary>
public sealed record CpiItem
{
    /// <summary>BLS item code string (e.g., <c>SA0</c>).</summary>
    public string ItemCode { get; init; } = "";

    /// <summary>Human-readable item description (e.g., <c>All items</c>).</summary>
    public string ItemName { get; init; } = "";
}

/// <summary>BLS CPI geographic area reference record.</summary>
public sealed record CpiArea
{
    /// <summary>BLS area code (e.g., <c>0000</c> for U.S. city average).</summary>
    public string AreaCode { get; init; } = "";

    /// <summary>Human-readable area name (e.g., <c>U.S. city average</c>).</summary>
    public string AreaName { get; init; } = "";
}

/// <summary>BLS CPI periodicity reference record.</summary>
public sealed record CpiPeriodicity
{
    /// <summary>Single-character periodicity code: <c>M</c>, <c>S</c>, or <c>A</c>.</summary>
    public string PeriodicityCode { get; init; } = "";

    /// <summary>Human-readable name (e.g., <c>Monthly</c>).</summary>
    public string PeriodicityName { get; init; } = "";
}

/// <summary>BLS CPI seasonal adjustment reference record.</summary>
public sealed record CpiSeasonalAdjustment
{
    /// <summary>Single-character code: <c>S</c> = seasonally adjusted, <c>U</c> = unadjusted.</summary>
    public string SeasonalCode { get; init; } = "";

    /// <summary>Human-readable name (e.g., <c>Seasonally Adjusted</c>).</summary>
    public string SeasonalName { get; init; } = "";
}
