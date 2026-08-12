using System.Globalization;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NationalWid.Ingestion.Models;

namespace NationalWid.Ingestion.Services;

public sealed class BlsFlatFileService(
    HttpClient httpClient,
    ILogger logger,
    SourceHashService? sourceHashService = null,
    string? connectionString = null)
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("BLS_FLATFILE_BASE_URL")
        ?? "https://download.bls.gov/pub/time.series";

    public async Task<List<BlsSeries>> TryLoadSeriesAsync(
        string dataset,
        string dataFile,
        IEnumerable<string> seriesIds,
        int startYear,
        int endYear,
        CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/{dataset}/{dataFile}";
        var seriesSet = new HashSet<string>(seriesIds, StringComparer.OrdinalIgnoreCase);
        var bySeries = new Dictionary<string, List<BlsDataPoint>>(StringComparer.OrdinalIgnoreCase);

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
        request.Headers.TryAddWithoutValidation("User-Agent", "ULMITA/1.0 (ulmita.org; mattsteadman@utah.gov)");
        request.Headers.TryAddWithoutValidation("Accept", "*/*");
        request.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
        request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");
        request.Headers.TryAddWithoutValidation("Pragma", "no-cache");
        request.Headers.Connection.Add("keep-alive");

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "BLS flat-file request failed: {Url}", url);
            return [];
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("BLS flat-file request returned {StatusCode} for {Url}", response.StatusCode, url);
            return [];
        }

        var tempFile = Path.GetTempFileName();
        try
        {
            await using (var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var fileStream = File.Create(tempFile))
            {
                await sourceStream.CopyToAsync(fileStream, cancellationToken);
            }

            var sourceHash = await SourceHashService.ComputeFileSha256HexAsync(tempFile, cancellationToken);
            var fileInfo = new FileInfo(tempFile);
            var datasetKey = $"BLS-FLAT-{dataset.ToUpperInvariant()}";

            var changed = true;
            if (sourceHashService is not null && !string.IsNullOrWhiteSpace(connectionString))
            {
                changed = await sourceHashService.ShouldProcessAsync(
                    connectionString,
                    datasetKey,
                    url,
                    sourceHash,
                    fileInfo.Length,
                    cancellationToken);
            }

            if (!changed)
            {
                return [];
            }

            await using var rawFile = File.OpenRead(tempFile);
            await using var contentStream = CreateDecodedStream(rawFile, response.Content.Headers.ContentEncoding);
            using var reader = new StreamReader(contentStream);

            var lineNumber = 0;
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (lineNumber == 1 && line.Contains("series_id", StringComparison.OrdinalIgnoreCase))
                    continue;

                var columns = SplitColumns(line);
                if (columns.Length < 4)
                    continue;

                var seriesId = columns[0];
                if (!seriesSet.Contains(seriesId))
                    continue;

                if (!int.TryParse(columns[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var year))
                    continue;

                if (year < startYear || year > endYear)
                    continue;

                var period = columns[2];
                var value = columns[3];
                var footnoteText = columns.Length > 4 ? columns[4] : string.Empty;

                var point = new BlsDataPoint
                {
                    Year = year.ToString(CultureInfo.InvariantCulture),
                    Period = period,
                    Value = value,
                    Footnotes = footnoteText.Contains('P', StringComparison.OrdinalIgnoreCase)
                        ? [new BlsFootnote { Code = "P" }]
                        : [],
                };

                if (!bySeries.TryGetValue(seriesId, out var points))
                {
                    points = [];
                    bySeries[seriesId] = points;
                }

                points.Add(point);
            }
        }
        finally
        {
            try
            {
                File.Delete(tempFile);
            }
            catch
            {
                // Ignore cleanup failures for temp files.
            }
        }

        var result = new List<BlsSeries>();
        foreach (var seriesId in seriesIds)
        {
            if (!bySeries.TryGetValue(seriesId, out var points))
                continue;

            points.Sort((a, b) =>
            {
                var yearCompare = string.CompareOrdinal(a.Year, b.Year);
                if (yearCompare != 0)
                    return yearCompare;
                return string.CompareOrdinal(a.Period, b.Period);
            });

            result.Add(new BlsSeries
            {
                SeriesId = seriesId,
                Data = points,
            });
        }

        logger.LogInformation(
            "Loaded {SeriesCount} series from BLS flat file {DataFile} ({Dataset})",
            result.Count,
            dataFile,
            dataset);

        return result;
    }

    public async Task ProcessDataFileAsync(
        string dataset,
        string dataFile,
        int startYear,
        int endYear,
        Func<string, bool> seriesFilter,
        Func<string, BlsDataPoint, ValueTask> onDataPoint,
        CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/{dataset}/{dataFile}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.CacheControl = new System.Net.Http.Headers.CacheControlHeaderValue { NoCache = true };
        request.Headers.TryAddWithoutValidation("User-Agent", "ULMITA/1.0 (ulmita.org; mattsteadman@utah.gov)");
        request.Headers.TryAddWithoutValidation("Accept", "*/*");
        request.Headers.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
        request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate, br");
        request.Headers.TryAddWithoutValidation("Pragma", "no-cache");
        request.Headers.Connection.Add("keep-alive");

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "BLS flat-file request failed: {Url}", url);
            return;
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("BLS flat-file request returned {StatusCode} for {Url}", response.StatusCode, url);
            return;
        }

        var tempFile = Path.GetTempFileName();
        try
        {
            await using (var sourceStream = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var fileStream = File.Create(tempFile))
            {
                await sourceStream.CopyToAsync(fileStream, cancellationToken);
            }

            var sourceHash = await SourceHashService.ComputeFileSha256HexAsync(tempFile, cancellationToken);
            var fileInfo = new FileInfo(tempFile);
            var datasetKey = $"BLS-FLAT-{dataset.ToUpperInvariant()}";

            var changed = true;
            if (sourceHashService is not null && !string.IsNullOrWhiteSpace(connectionString))
            {
                changed = await sourceHashService.ShouldProcessAsync(
                    connectionString,
                    datasetKey,
                    url,
                    sourceHash,
                    fileInfo.Length,
                    cancellationToken);
            }

            if (!changed)
            {
                return;
            }

            await using var rawFile = File.OpenRead(tempFile);
            await using var contentStream = CreateDecodedStream(rawFile, response.Content.Headers.ContentEncoding);
            using var reader = new StreamReader(contentStream);

            var lineNumber = 0;
            var matchedRows = 0;
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                lineNumber++;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (lineNumber == 1 && line.Contains("series_id", StringComparison.OrdinalIgnoreCase))
                    continue;

                var columns = SplitColumns(line);
                if (columns.Length < 4)
                    continue;

                var seriesId = columns[0];
                if (!seriesFilter(seriesId))
                    continue;

                if (!int.TryParse(columns[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var year))
                    continue;

                if (year < startYear || year > endYear)
                    continue;

                var period = columns[2];
                var value = columns[3];
                var footnoteText = columns.Length > 4 ? columns[4] : string.Empty;

                var point = new BlsDataPoint
                {
                    Year = year.ToString(CultureInfo.InvariantCulture),
                    Period = period,
                    Value = value,
                    Footnotes = footnoteText.Contains('P', StringComparison.OrdinalIgnoreCase)
                        ? [new BlsFootnote { Code = "P" }]
                        : [],
                };

                await onDataPoint(seriesId, point);
                matchedRows++;
            }

            logger.LogInformation(
                "Processed {MatchedRows} rows from BLS flat file {DataFile} ({Dataset})",
                matchedRows,
                dataFile,
                dataset);
        }
        finally
        {
            try
            {
                File.Delete(tempFile);
            }
            catch
            {
                // Ignore cleanup failures for temp files.
            }
        }
    }

    private static string[] SplitColumns(string line)
    {
        if (line.Contains('\t'))
            return line.Split('\t', StringSplitOptions.TrimEntries);

        // Fallback for environments where tabs may be converted to spaces.
        return Regex.Split(line.Trim(), "\\s+");
    }

    private static Stream CreateDecodedStream(Stream sourceStream, ICollection<string> contentEncodings)
    {
        if (contentEncodings.Any(e => e.Equals("gzip", StringComparison.OrdinalIgnoreCase)))
            return new GZipStream(sourceStream, CompressionMode.Decompress);

        if (contentEncodings.Any(e => e.Equals("deflate", StringComparison.OrdinalIgnoreCase)))
            return new DeflateStream(sourceStream, CompressionMode.Decompress);

        if (contentEncodings.Any(e => e.Equals("br", StringComparison.OrdinalIgnoreCase)))
            return new BrotliStream(sourceStream, CompressionMode.Decompress);

        return sourceStream;
    }
}