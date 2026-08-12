using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace NationalWid.Api.Services;

public enum DownloadFormat
{
    Json,
    Csv,
    Xlsx,
    Tsv,
    Psv,
    JsonFile,
}

/// <summary>
/// Negotiates the response format (<c>?format=</c> query parameter wins over the
/// <c>Accept</c> header) and renders CSV/XLSX downloads for any model type.
/// </summary>
public interface IDownloadService
{
    DownloadFormat ResolveFormat(HttpRequest request, string? format);

    IActionResult CsvResult<T>(IReadOnlyList<T> rows, string fileBaseName);

    IActionResult TsvResult<T>(IReadOnlyList<T> rows, string fileBaseName);

    IActionResult PsvResult<T>(IReadOnlyList<T> rows, string fileBaseName);

    IActionResult JsonFileResult<T>(IReadOnlyList<T> rows, string fileBaseName);

    IActionResult XlsxResult<T>(IReadOnlyList<T> rows, string fileBaseName);
}

public sealed class DownloadService : IDownloadService
{
    public const string CsvContentType  = "text/csv";
    public const string TsvContentType  = "text/tab-separated-values";
    public const string PsvContentType  = "text/plain";
    public const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string JsonContentType = "application/json";

    private static readonly ConcurrentDictionary<Type, ExportColumn[]> ColumnCache = new();

    private sealed record ExportColumn(string Name, Func<object, object?> Getter);

    public DownloadFormat ResolveFormat(HttpRequest request, string? format)
    {
        if (!string.IsNullOrWhiteSpace(format))
        {
            return format.ToLowerInvariant() switch
            {
                "json"                  => DownloadFormat.Json,
                "jsonfile" or "json-dl" => DownloadFormat.JsonFile,
                "csv"                   => DownloadFormat.Csv,
                "tsv"                   => DownloadFormat.Tsv,
                "psv" or "txt"          => DownloadFormat.Psv,
                "xlsx" or "excel"       => DownloadFormat.Xlsx,
                _ => throw new BadHttpRequestException(
                    $"'{format}' is not a supported format. Use json, jsonfile, csv, tsv, psv, or xlsx."),
            };
        }

        var accept = request.Headers.Accept.ToString();
        if (accept.Contains(CsvContentType, StringComparison.OrdinalIgnoreCase))
            return DownloadFormat.Csv;
        if (accept.Contains(TsvContentType, StringComparison.OrdinalIgnoreCase))
            return DownloadFormat.Tsv;
        if (accept.Contains(XlsxContentType, StringComparison.OrdinalIgnoreCase))
            return DownloadFormat.Xlsx;

        return DownloadFormat.Json;
    }

    public IActionResult CsvResult<T>(IReadOnlyList<T> rows, string fileBaseName)
    {
        var columns = ColumnsFor<T>();
        using var writer = new StringWriter();
        using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        {
            foreach (var column in columns)
            {
                csv.WriteField(column.Name);
            }

            csv.NextRecord();

            foreach (var row in rows)
            {
                foreach (var column in columns)
                {
                    csv.WriteField(FormatCsvValue(column.Getter(row!)));
                }

                csv.NextRecord();
            }
        }

        return new FileContentResult(Encoding.UTF8.GetBytes(writer.ToString()), CsvContentType)
        {
            FileDownloadName = $"{fileBaseName}.csv",
        };
    }

    public IActionResult TsvResult<T>(IReadOnlyList<T> rows, string fileBaseName) =>
        DelimitedResult(rows, fileBaseName, '\t', TsvContentType, "tsv");

    public IActionResult PsvResult<T>(IReadOnlyList<T> rows, string fileBaseName) =>
        DelimitedResult(rows, fileBaseName, '|', PsvContentType, "txt");

    private IActionResult DelimitedResult<T>(
        IReadOnlyList<T> rows, string fileBaseName, char delimiter, string contentType, string extension)
    {
        var columns = ColumnsFor<T>();
        var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter.ToString(),
        };
        using var writer = new StringWriter();
        using (var csv = new CsvWriter(writer, cfg))
        {
            foreach (var column in columns) csv.WriteField(column.Name);
            csv.NextRecord();
            foreach (var row in rows)
            {
                foreach (var column in columns) csv.WriteField(FormatCsvValue(column.Getter(row!)));
                csv.NextRecord();
            }
        }
        return new FileContentResult(Encoding.UTF8.GetBytes(writer.ToString()), contentType)
        {
            FileDownloadName = $"{fileBaseName}.{extension}",
        };
    }

    public IActionResult JsonFileResult<T>(IReadOnlyList<T> rows, string fileBaseName)
    {
        var options = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false,
        };
        var json = JsonSerializer.Serialize(rows, options);
        return new FileContentResult(Encoding.UTF8.GetBytes(json), JsonContentType)
        {
            FileDownloadName = $"{fileBaseName}.json",
        };
    }

    public IActionResult XlsxResult<T>(IReadOnlyList<T> rows, string fileBaseName)
    {
        var columns = ColumnsFor<T>();
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Data");

        for (var c = 0; c < columns.Length; c++)
        {
            sheet.Cell(1, c + 1).Value = columns[c].Name;
            sheet.Cell(1, c + 1).Style.Font.Bold = true;
        }

        for (var r = 0; r < rows.Count; r++)
        {
            for (var c = 0; c < columns.Length; c++)
            {
                sheet.Cell(r + 2, c + 1).Value = ToCellValue(columns[c].Getter(rows[r]!));
            }
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return new FileContentResult(stream.ToArray(), XlsxContentType)
        {
            FileDownloadName = $"{fileBaseName}.xlsx",
        };
    }

    private static ExportColumn[] ColumnsFor<T>() =>
        ColumnCache.GetOrAdd(typeof(T), static type =>
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .Select(p => new ExportColumn(
                    p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                        ?? JsonNamingPolicy.CamelCase.ConvertName(p.Name),
                    p.GetValue))
                .ToArray());

    private static string? FormatValue(object? value) => value switch
    {
        null => null,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };

    private static XLCellValue ToCellValue(object? value) => value switch
    {
        null => Blank.Value,
        string s => SanitizeSpreadsheetText(s.TrimEnd()),
        decimal d => d,
        double d => d,
        float f => f,
        long l => l,
        int i => i,
        short s => s,
        bool b => b,
        DateTime dt => dt,
        _ => value.ToString(),
    };

    private static string? FormatCsvValue(object? value)
    {
        var formatted = FormatValue(value);
        if (formatted is null)
            return null;

        if (value is string or char)
        {
            // Trim PostgreSQL char(n) trailing-space padding before writing to CSV/TSV/etc.
            return SanitizeSpreadsheetText(formatted.TrimEnd());
        }

        return formatted;
    }

    private static bool NeedsFormulaProtection(string value)
    {
        if (value.Length == 0 || value[0] == '\'')
        {
            return false;
        }

        for (var i = 0; i < value.Length; i++)
        {
            if (char.IsWhiteSpace(value[i]))
            {
                continue;
            }

            return value[i] is '=' or '+' or '-' or '@';
        }

        return false;
    }

    private static string SanitizeSpreadsheetText(string value) =>
        NeedsFormulaProtection(value)
            ? $"'{value}"
            : value;
}
