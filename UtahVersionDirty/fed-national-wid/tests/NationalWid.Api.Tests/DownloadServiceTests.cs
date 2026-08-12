using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using NationalWid.Api.Services;

namespace NationalWid.Api.Tests;

public class DownloadServiceTests
{
    private static readonly DownloadService Service = new();

    [Fact]
    public void CsvResult_PreservesSafeAndSpecialText()
    {
        var rows = new[]
        {
            new DownloadRow("safe text", "value, with \"quotes\" and\nnew line"),
        };

        var result = Assert.IsType<FileContentResult>(Service.CsvResult(rows, "download"));
        var record = ReadSingleCsvRecord(result);

        Assert.Equal("safe text", record["name"]);
        Assert.Equal("value, with \"quotes\" and\nnew line", record["value"]);
    }

    [Theory]
    [InlineData("=2+2")]
    [InlineData("+cmd|' /C calc'!A0")]
    [InlineData("-10+20")]
    [InlineData("@SUM(A1:A2)")]
    [InlineData(" \t=NOW()")]
    [InlineData(" \t+1+2")]
    [InlineData(" \t-1+2")]
    [InlineData(" \t@SUM(A1:A2)")]
    public void CsvResult_PrefixesValuesThatLookLikeFormulae(string input)
    {
        var rows = new[]
        {
            new DownloadRow("row", input),
        };

        var result = Assert.IsType<FileContentResult>(Service.CsvResult(rows, "download"));
        var record = ReadSingleCsvRecord(result);

        Assert.Equal($"'{input}", record["value"]);
    }

    [Fact]
    public void CsvResult_DoesNotDoublePrefixWhenAlreadyEscaped()
    {
        var rows = new[]
        {
            new DownloadRow("row", "'=2+2"),
        };

        var result = Assert.IsType<FileContentResult>(Service.CsvResult(rows, "download"));
        var record = ReadSingleCsvRecord(result);

        Assert.Equal("'=2+2", record["value"]);
    }

    [Fact]
    public void CsvResult_DoesNotPrefixWhitespaceThenSafeText()
    {
        var rows = new[]
        {
            new DownloadRow("row", "  some text"),
        };

        var result = Assert.IsType<FileContentResult>(Service.CsvResult(rows, "download"));
        var record = ReadSingleCsvRecord(result);

        Assert.Equal("  some text", record["value"]);
    }

    [Fact]
    public void CsvResult_PreservesTypedNegativeNumbers()
    {
        var rows = new[]
        {
            new TypedDownloadRow(-42, -12.5m),
        };

        var result = Assert.IsType<FileContentResult>(Service.CsvResult(rows, "download"));
        var record = ReadSingleCsvRecord(result);

        Assert.Equal("-42", record["count"]);
        Assert.Equal("-12.5", record["rate"]);
    }

    [Theory]
    [InlineData("=2+2")]
    [InlineData("+cmd|' /C calc'!A0")]
    [InlineData("-10+20")]
    [InlineData("@SUM(A1:A2)")]
    [InlineData(" \t=NOW()")]
    public void XlsxResult_WritesFormulaLikeValuesAsText(string input)
    {
        var rows = new[]
        {
            new DownloadRow("row", input),
        };

        var result = Assert.IsType<FileContentResult>(Service.XlsxResult(rows, "download"));
        using var stream = new MemoryStream(result.FileContents);
        using var workbook = new XLWorkbook(stream);
        var valueCell = workbook.Worksheet("Data").Cell(2, 2);

        Assert.False(valueCell.HasFormula);
        Assert.Equal(XLDataType.Text, valueCell.DataType);
    }

    private static Dictionary<string, string?> ReadSingleCsvRecord(FileContentResult result)
    {
        using var reader = new StringReader(Encoding.UTF8.GetString(result.FileContents));
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        Assert.True(csv.Read());
        csv.ReadHeader();
        Assert.True(csv.Read());

        return csv.HeaderRecord!
            .ToDictionary(header => header, header => csv.GetField(header), StringComparer.Ordinal);
    }

    public sealed record DownloadRow(string Name, string Value);
    public sealed record TypedDownloadRow(int Count, decimal Rate);
}
