namespace NationalWid.Api.Services;

/// <summary>
/// Normalization and validation helpers for WID 3.0 code values.
///
/// Design conventions:
/// - Area codes are accepted bare and left-padded with '0' to 6 characters.
/// - Industry and occupation codes are stored trimmed (no hyphens) in this database,
///   so filter input is trimmed and de-hyphenated before use.
/// - State FIPS codes must be exactly 2 decimal digits.
/// </summary>
public static class WidCodes
{
    /// <summary>National-level state FIPS used for all nationally published data.</summary>
    public const string NationalStFips = "00";

    /// <summary>
    /// Trims whitespace from <paramref name="stFips"/>; returns <c>null</c> when blank.
    /// Does not validate — call <see cref="IsValidStFips"/> separately when validation is required.
    /// </summary>
    public static string? NormalizeStFips(string? stFips)
    {
        var trimmed = stFips?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>Returns <c>true</c> when <paramref name="stFips"/> is exactly 2 decimal digits.</summary>
    public static bool IsValidStFips(string stFips) =>
        stFips.Length == 2 && stFips.All(char.IsDigit);

    /// <summary>
    /// Parses a comma-separated <paramref name="raw"/> stFips filter string into a validated array
    /// ready to use in a <c>.Where(x => filter.Contains(x.StFips))</c> clause.
    ///
    /// Returns an empty array when <paramref name="raw"/> is blank (no filter to apply).
    /// Throws <see cref="BadHttpRequestException"/> with a clear message when any value
    /// does not match the 2-digit FIPS format.
    /// </summary>
    public static string[] ParseStFipsFilter(string? raw)
    {
        var values = QueryFiltering
            .SplitCsv(raw)
            .Select(NormalizeStFips)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v!)
            .ToArray();

        if (values.Any(v => !IsValidStFips(v)))
            throw new BadHttpRequestException("stFips must be a 2-digit numeric code.");

        return values;
    }

    /// <summary>
    /// Left-pads <paramref name="area"/> to 6 characters with <c>'0'</c>.
    /// Returns <c>null</c> when blank so callers can skip the filter entirely.
    /// </summary>
    public static string? NormalizeArea(string? area)
    {
        if (string.IsNullOrWhiteSpace(area))
            return null;

        var trimmed = area.Trim();
        return trimmed.Length < 6 ? trimmed.PadLeft(6, '0') : trimmed;
    }

    /// <summary>
    /// Removes hyphens and whitespace from industry/occupation code values so that both
    /// SOC display format ("15-0000") and WID storage format ("150000") are accepted as input.
    /// Returns <c>null</c> when blank.
    /// </summary>
    public static string? NormalizeCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var trimmed = code.Trim().Replace("-", "", StringComparison.Ordinal);
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    /// <summary>
    /// Same as <see cref="NormalizeCode"/> but preserves wildcard characters (<c>*</c>)
    /// so that patterns like <c>15*</c> and <c>*0000</c> survive normalization for wildcard filter use.
    /// </summary>
    public static string? NormalizeCodePattern(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var trimmed = code.Trim().Replace("-", "", StringComparison.Ordinal);
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
