using System.Globalization;

namespace NationalWid.Ingestion.Services;

/// <summary>
/// Maps BLS period codes to WID (PeriodType, Period) pairs.
/// WID standard period types: '01' = annual, '02' = quarterly, '03' = monthly.
/// </summary>
public static class BlsPeriodMapper
{
    public static bool TryMap(string blsPeriod, out string periodType, out string period)
    {
        periodType = "";
        period = "";

        if (string.IsNullOrEmpty(blsPeriod) || blsPeriod.Length < 3)
        {
            return false;
        }

        var kind = char.ToUpperInvariant(blsPeriod[0]);
        var number = blsPeriod[1..];

        switch (kind)
        {
            case 'M' when number == "13":
                // M13 is the BLS annual average.
                periodType = "01";
                period = "00";
                return true;
            case 'M' when int.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out var month) && month is >= 1 and <= 12:
                periodType = "03";
                period = month.ToString("00");
                return true;
            case 'Q' when int.TryParse(number, NumberStyles.Integer, CultureInfo.InvariantCulture, out var quarter) && quarter is >= 1 and <= 5:
                if (quarter == 5)
                {
                    // Q05 is the BLS annual average for quarterly series.
                    periodType = "01";
                    period = "00";
                }
                else
                {
                    periodType = "02";
                    period = quarter.ToString("00");
                }

                return true;
            case 'A':
                periodType = "01";
                period = "00";
                return true;
            default:
                return false;
        }
    }
}
