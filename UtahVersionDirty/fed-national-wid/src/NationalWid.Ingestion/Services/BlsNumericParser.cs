using System.Globalization;

namespace NationalWid.Ingestion.Services;

public static class BlsNumericParser
{
    public static bool TryParseDecimal(string? value, out decimal parsed) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out parsed);
}
