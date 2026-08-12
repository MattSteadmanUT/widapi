using NationalWid.Ingestion;

namespace NationalWid.Ingestion.Tests;

public sealed class ParameterPathTests
{
    private const string VariableName = "NATIONAL_WID_TEST_PARAMETER_PATH";

    [Fact]
    public void GetParameterPath_ReturnsFallback_WhenVariableIsMissing()
    {
        WithEnvironmentVariable(null, () =>
            Assert.Equal("/wid-api/fallback", Function.GetParameterPath(VariableName, "/wid-api/fallback")));
    }

    [Fact]
    public void GetParameterPath_ReturnsFallback_WhenVariableIsWhitespace()
    {
        WithEnvironmentVariable("  ", () =>
            Assert.Equal("/wid-api/fallback", Function.GetParameterPath(VariableName, "/wid-api/fallback")));
    }

    [Fact]
    public void GetParameterPath_ReturnsTrimmedConfiguredPath()
    {
        WithEnvironmentVariable("  /dev/wid-api/db-connection-string  ", () =>
            Assert.Equal(
                "/dev/wid-api/db-connection-string",
                Function.GetParameterPath(VariableName, "/wid-api/fallback")));
    }

    private static void WithEnvironmentVariable(string? value, Action assertion)
    {
        var original = Environment.GetEnvironmentVariable(VariableName);
        try
        {
            Environment.SetEnvironmentVariable(VariableName, value);
            assertion();
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, original);
        }
    }
}
