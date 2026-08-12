using System.Globalization;
using NationalWid.Ingestion.Services;

namespace NationalWid.Ingestion.Tests;

public sealed class BlsNumericParserTests
{
    [Fact]
    public void TryParseDecimal_UsesInvariantCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");

            var ok = BlsNumericParser.TryParseDecimal("1234.56", out var value);

            Assert.True(ok);
            Assert.Equal(1234.56m, value);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
