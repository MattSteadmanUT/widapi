namespace NationalWid.Api.Services;

public static class MetadataFields
{
    private static readonly HashSet<string> Supported = new(StringComparer.OrdinalIgnoreCase)
    {
        "areas",
        "years",
        "periods",
        "minPeriod",
        "maxPeriod",
        "projectedYears",
    };

    public static HashSet<string> Parse(string? raw)
    {
        var fields = QueryFiltering.SplitCsv(raw)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (fields.Count == 0)
            return Supported.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var invalid = fields.Where(x => !Supported.Contains(x)).ToArray();
        if (invalid.Length > 0)
        {
            throw new BadHttpRequestException(
                $"metadataFields contains unsupported value(s): {string.Join(", ", invalid)}.");
        }

        return fields;
    }

    public static string? GetProjectionStartYear(string? projectionsPeriod)
    {
        if (string.IsNullOrWhiteSpace(projectionsPeriod))
            return null;

        var parts = projectionsPeriod.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return null;

        return parts[0].Length == 4 && parts[0].All(char.IsDigit) ? parts[0] : null;
    }

    public static string? GetProjectionEndYear(string? projectionsPeriod)
    {
        if (string.IsNullOrWhiteSpace(projectionsPeriod))
            return null;

        var parts = projectionsPeriod.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return null;

        return parts[1].Length == 4 && parts[1].All(char.IsDigit) ? parts[1] : null;
    }
}
