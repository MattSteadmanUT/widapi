using System.Globalization;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;
using NationalWid.Ingestion.Services;
using Npgsql;

namespace NationalWid.Ingestion.Ingestors;

/// <summary>
/// Loads non-core lookup reference data directly from data.widcenter.org so lookup endpoints
/// are populated from canonical WIDCenter content instead of sparse derivations.
/// </summary>
public sealed class WidCenterLookupIngestor(
    HttpClient httpClient,
    string connectionString,
    SourceHashService sourceHashService,
    ILogger logger)
{
    private const string GeogZipUrl = "https://data.widcenter.org/wfinfodb/national/geog15.zip";
    private const string AreaTypeUrl = "https://data.widcenter.org/wfinfodb/structure/lookup/areatype.txt";
    private const string CesCodeUrl = "https://data.widcenter.org/wfinfodb/national/cescode.txt";
    private const string IndustryCodesUrl = "https://data.widcenter.org/download/naics2022/indcodes2022.csv";
    private const string OccupationCodesUrl = "https://data.widcenter.org/download/soc2018/occcodes.csv";
    private const string ReferenceProjPeriod = "0000-0000";

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var rows = 0;

        rows += await LoadGeographyBundleAsync(cancellationToken);
        rows += await LoadAreaTypesAsync(cancellationToken);
        rows += await LoadCesCodesAsync(cancellationToken);
        rows += await LoadIndustryDirectoriesAsync(cancellationToken);
        rows += await LoadOccupationDirectoriesAsync(cancellationToken);

        return rows;
    }

    private async Task<int> LoadGeographyBundleAsync(CancellationToken cancellationToken)
    {
        var bytes = await DownloadChangedSourceAsync("WIDCENTER-GEOG", GeogZipUrl, cancellationToken);
        if (bytes is null)
        {
            return 0;
        }

        await using var stream = new MemoryStream(bytes);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
        var nested = zip.Entries.FirstOrDefault(e => e.FullName.EndsWith("geog15txt.zip", StringComparison.OrdinalIgnoreCase));
        if (nested is null)
        {
            logger.LogWarning("WIDCenter geography zip did not contain geog15txt.zip");
            return 0;
        }

        await using var nestedStream = new MemoryStream();
        await using (var nestedSource = nested.Open())
        {
            await nestedSource.CopyToAsync(nestedStream, cancellationToken);
        }
        nestedStream.Position = 0;

        using var nestedZip = new ZipArchive(nestedStream, ZipArchiveMode.Read, leaveOpen: false);
        var geogEntry = nestedZip.Entries.FirstOrDefault(e => e.FullName.EndsWith("geog.txt", StringComparison.OrdinalIgnoreCase));
        var stateEntry = nestedZip.Entries.FirstOrDefault(e => e.FullName.EndsWith("stfipstb.txt", StringComparison.OrdinalIgnoreCase));
        var areaTypeEntry = nestedZip.Entries.FirstOrDefault(e => e.FullName.EndsWith("AREATYPE.txt", StringComparison.OrdinalIgnoreCase));

        if (geogEntry is null || stateEntry is null)
        {
            logger.LogWarning("WIDCenter geography text bundle missing expected files (geog.txt or stfipstb.txt)");
            return 0;
        }

        var areaTypeTitles = new Dictionary<(string StFips, string AreaType), string>();
        if (areaTypeEntry is not null)
        {
            await foreach (var line in ReadLinesAsync(areaTypeEntry, cancellationToken))
            {
                var cols = ParseCsvLine(line);
                if (cols.Length < 3)
                    continue;

                var st = NormalizeCode(cols[0], 2);
                var at = NormalizeCode(cols[1], 2);
                var title = cols[2].Trim();
                if (string.IsNullOrWhiteSpace(st) || string.IsNullOrWhiteSpace(at) || string.IsNullOrWhiteSpace(title))
                    continue;

                areaTypeTitles[(st, at)] = title;
            }
        }

        var geographies = new List<(string StFips, string AreaType, string AreaTypeVersion, string Area, string AreaTitle, string? AreaTypeTitle)>();
        await foreach (var line in ReadLinesAsync(geogEntry, cancellationToken))
        {
            var cols = ParseCsvLine(line);
            if (cols.Length < 4)
                continue;

            var st = NormalizeCode(cols[0], 2);
            var at = NormalizeCode(cols[1], 2);
            var rawArea = NormalizeDigits(cols[2], 1, 6); // 6-digit WIDCenter area code
            // col[3] is a 60-char truncated title; col[4] is the full name — use the longer one.
            var col3 = cols[3].Trim();
            var col4 = cols.Length > 4 ? cols[4].Trim() : "";
            var areaTitle = (col4.Length > col3.Length ? col4 : col3)[..Math.Min((col4.Length > col3.Length ? col4 : col3).Length, 80)];
            // col[4] is NOT the area type title — it's the full area name.
            // Get area type title from the AREATYPE.txt dictionary instead.
            string? areaTypeTitle = null;

            if (string.IsNullOrWhiteSpace(st) || string.IsNullOrWhiteSpace(at) || string.IsNullOrWhiteSpace(rawArea) || string.IsNullOrWhiteSpace(areaTitle))
                continue;

            // Convert WIDCenter area codes to WID 3.0 char(6) convention.
            var area = at switch
            {
                "00" => "000000",                    // National
                "01" => st.PadLeft(6, '0'),            // State: stFips left-padded to 6
                "04" => CountyArea(st, rawArea),       // County: suffix padded to 6
                _    => rawArea.PadLeft(6, '0'),       // MSA/sub-state: WIDCenter code padded to 6
            };

            if (areaTypeTitles.TryGetValue((st, at), out var stTitle))
                areaTypeTitle = stTitle;
            else if (areaTypeTitles.TryGetValue(("00", at), out var nationalTitle))
                areaTypeTitle = nationalTitle;

            geographies.Add((st, at, "0", area, areaTitle, areaTypeTitle));
        }

        var states = new List<(string StFips, string StateName, string StateAbbrev)>();
        await foreach (var line in ReadLinesAsync(stateEntry, cancellationToken))
        {
            var cols = ParseCsvLine(line);
            if (cols.Length < 3)
                continue;

            var st = NormalizeCode(cols[0], 2);
            var name = cols[1].Trim();
            var abbrev = cols[2].Trim();

            if (string.IsNullOrWhiteSpace(st) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(abbrev))
                continue;

            states.Add((st, name, abbrev));
        }

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);

        var affected = 0;
        affected += await UpsertGeographiesAsync(conn, geographies, cancellationToken);
        affected += await UpsertStateFipsAsync(conn, states, cancellationToken);

        return affected;
    }

    private async Task<int> LoadAreaTypesAsync(CancellationToken cancellationToken)
    {
        var bytes = await DownloadChangedSourceAsync("WIDCENTER-AREATYPE", AreaTypeUrl, cancellationToken);
        if (bytes is null)
        {
            return 0;
        }

        var lines = ReadUtf8Lines(bytes);
        var rows = new List<(string StFips, string AreaType, string AreaTypeName)>();

        foreach (var line in lines)
        {
            var cols = ParseCsvLine(line);
            if (cols.Length < 3)
                continue;

            var st = NormalizeCode(cols[0], 2);
            var at = NormalizeCode(cols[1], 2);
            var name = cols[2].Trim();
            if (string.IsNullOrWhiteSpace(st) || string.IsNullOrWhiteSpace(at) || string.IsNullOrWhiteSpace(name))
                continue;

            rows.Add((st, at, name));
        }

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);
        return await UpsertAreaTypesAsync(conn, rows, cancellationToken);
    }

    private async Task<int> LoadCesCodesAsync(CancellationToken cancellationToken)
    {
        var bytes = await DownloadChangedSourceAsync("WIDCENTER-CESCODE", CesCodeUrl, cancellationToken);
        if (bytes is null)
        {
            return 0;
        }

        var lines = ReadUtf8Lines(bytes);
        var rows = new List<(string StFips, string SeriesCodeType, string SeriesCode, string SeriesTitle)>();

        foreach (var line in lines)
        {
            var cols = ParseCsvLine(line);
            if (cols.Length < 3)
                continue;

            var st = NormalizeCode(cols[0], 2);
            var seriesCode = NormalizeCode(cols[1], minDigits: 1, maxDigits: 32);
            var title = cols.Length > 3 ? cols[3].Trim() : cols[2].Trim();

            if (string.IsNullOrWhiteSpace(st) || string.IsNullOrWhiteSpace(seriesCode))
                continue;

            var seriesCodeType = seriesCode.Length >= 2 ? seriesCode[..2] : "00";
            rows.Add((st, seriesCodeType, seriesCode, title));
        }

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);
        return await UpsertCesCodesAsync(conn, rows, cancellationToken);
    }

    private async Task<int> LoadIndustryDirectoriesAsync(CancellationToken cancellationToken)
    {
        var bytes = await DownloadChangedSourceAsync("WIDCENTER-INDCODES2022", IndustryCodesUrl, cancellationToken);
        if (bytes is null)
        {
            return 0;
        }

        var rowsByKey = new Dictionary<string, (string StFips, string ProjPeriod, string IndCodeType, string IndCode, string IndTitle)>(StringComparer.Ordinal);
        foreach (var line in ReadUtf8Lines(bytes))
        {
            var cols = ParseCsvLine(line);
            if (cols.Length < 4)
                continue;

            var st = NormalizeCode(cols[0], 2);
            var codeType = NormalizeCode(cols[1], 2);
            var code = NormalizeDigits(cols[2], 1, 10);
            var title = cols[3].Trim();

            if (string.IsNullOrWhiteSpace(st) || string.IsNullOrWhiteSpace(codeType) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(title))
                continue;

            var key = $"{st}:{ReferenceProjPeriod}:{codeType}:{code}";
            rowsByKey[key] = (st, ReferenceProjPeriod, codeType, code, title);
        }

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);
        return await UpsertIndDirectoriesAsync(conn, rowsByKey.Values.ToList(), cancellationToken);
    }

    private async Task<int> LoadOccupationDirectoriesAsync(CancellationToken cancellationToken)
    {
        var bytes = await DownloadChangedSourceAsync("WIDCENTER-OCCCODES2018", OccupationCodesUrl, cancellationToken);
        if (bytes is null)
        {
            return 0;
        }

        var rowsByKey = new Dictionary<string, (string StFips, string ProjPeriod, string OccCodeType, string OccCode, string OccTitle)>(StringComparer.Ordinal);
        foreach (var line in ReadUtf8Lines(bytes))
        {
            var cols = ParseCsvLine(line);
            if (cols.Length < 4)
                continue;

            var st = NormalizeCode(cols[0], 2);
            var codeType = NormalizeCode(cols[1], 2);
            var code = NormalizeDigits(cols[2], 1, 10);
            var title = cols[3].Trim();

            if (string.IsNullOrWhiteSpace(st) || string.IsNullOrWhiteSpace(codeType) || string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(title))
                continue;

            var key = $"{st}:{ReferenceProjPeriod}:{codeType}:{code}";
            rowsByKey[key] = (st, ReferenceProjPeriod, codeType, code, title);
        }

        await using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync(cancellationToken);
        return await UpsertOccDirectoriesAsync(conn, rowsByKey.Values.ToList(), cancellationToken);
    }

    private async Task<byte[]?> DownloadChangedSourceAsync(
        string dataset,
        string url,
        CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("WIDCenter source request failed: {Url} ({StatusCode})", url, response.StatusCode);
            return null;
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var hash = SourceHashService.ComputeSha256Hex(bytes);
        var changed = await sourceHashService.ShouldProcessAsync(
            connectionString,
            dataset,
            url,
            hash,
            bytes.LongLength,
            cancellationToken);

        return changed ? bytes : null;
    }

    private static IEnumerable<string> ReadUtf8Lines(byte[] bytes)
    {
        using var reader = new StringReader(Encoding.UTF8.GetString(bytes));
        string? line;
        var isFirstLine = true;
        while ((line = reader.ReadLine()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (isFirstLine && line.Contains("stfips", StringComparison.OrdinalIgnoreCase))
            {
                isFirstLine = false;
                continue;
            }

            isFirstLine = false;
            yield return line;
        }
    }

    private static async IAsyncEnumerable<string> ReadLinesAsync(
        ZipArchiveEntry entry,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var stream = entry.Open();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(line))
                continue;

            yield return line;
        }
    }

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString().Trim());
                sb.Clear();
                continue;
            }

            sb.Append(c);
        }

        result.Add(sb.ToString().Trim());
        return result.ToArray();
    }

    private static string CountyArea(string stFips, string rawArea)
    {
        // County area = last 3 digits of WIDCenter area code (= county FIPS suffix), padded to 6.
        var padded = rawArea.PadLeft(6, '0');
        return padded[^3..].PadLeft(6, '0');
    }

    private static string NormalizeArea(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits))
            return string.Empty;

        if (digits.Length > 7)
            digits = digits[^7..];

        return digits.PadLeft(7, '0');
    }

    private static string NormalizeCode(string value, int minDigits = 1, int maxDigits = 2)
    {
        var digits = new string(value.Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits))
            return string.Empty;

        if (digits.Length > maxDigits)
            return digits[..maxDigits];

        if (digits.Length < minDigits)
            return digits.PadLeft(minDigits, '0');

        return digits;
    }

    private static string NormalizeDigits(string value, int minDigits = 1, int maxDigits = 10)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (string.IsNullOrWhiteSpace(digits))
            return string.Empty;

        if (digits.Length > maxDigits)
            digits = digits[..maxDigits];

        if (digits.Length < minDigits)
            digits = digits.PadLeft(minDigits, '0');

        return digits;
    }

    private static async Task<int> UpsertAreaTypesAsync(
        NpgsqlConnection conn,
        IEnumerable<(string StFips, string AreaType, string AreaTypeName)> rows,
        CancellationToken cancellationToken)
    {
        var affected = 0;
        foreach (var chunk in rows.Chunk(500))
        {
            await using var tx = await conn.BeginTransactionAsync(cancellationToken);
            await using var batch = new NpgsqlBatch(conn, tx);

            foreach (var row in chunk)
            {
                var cmd = new NpgsqlBatchCommand(@"
                    INSERT INTO areatypes (stfips, areatype, areatypename)
                    VALUES (@stfips, @areatype, @areatypename)
                    ON CONFLICT (stfips, areatype)
                    DO UPDATE SET
                        areatypename = EXCLUDED.areatypename");

                cmd.Parameters.AddWithValue("stfips", row.StFips);
                cmd.Parameters.AddWithValue("areatype", row.AreaType);
                cmd.Parameters.AddWithValue("areatypename", row.AreaTypeName);
                batch.BatchCommands.Add(cmd);
            }

            affected += await batch.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }

        return affected;
    }

    private static async Task<int> UpsertStateFipsAsync(
        NpgsqlConnection conn,
        IEnumerable<(string StFips, string StateName, string StateAbbrev)> rows,
        CancellationToken cancellationToken)
    {
        var affected = 0;
        foreach (var chunk in rows.Chunk(500))
        {
            await using var tx = await conn.BeginTransactionAsync(cancellationToken);
            await using var batch = new NpgsqlBatch(conn, tx);

            foreach (var row in chunk)
            {
                var cmd = new NpgsqlBatchCommand(@"
                    INSERT INTO statefips (stfips, statename, stateabbrev)
                    VALUES (@stfips, @statename, @stateabbrev)
                    ON CONFLICT (stfips)
                    DO UPDATE SET
                        statename = EXCLUDED.statename,
                        stateabbrev = EXCLUDED.stateabbrev");

                cmd.Parameters.AddWithValue("stfips", row.StFips);
                cmd.Parameters.AddWithValue("statename", row.StateName);
                cmd.Parameters.AddWithValue("stateabbrev", row.StateAbbrev);
                batch.BatchCommands.Add(cmd);
            }

            affected += await batch.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }

        return affected;
    }

    private static async Task<int> UpsertGeographiesAsync(
        NpgsqlConnection conn,
        IEnumerable<(string StFips, string AreaType, string AreaTypeVersion, string Area, string AreaTitle, string? AreaTypeTitle)> rows,
        CancellationToken cancellationToken)
    {
        var affected = 0;
        foreach (var chunk in rows.Chunk(500))
        {
            await using var tx = await conn.BeginTransactionAsync(cancellationToken);
            await using var batch = new NpgsqlBatch(conn, tx);

            foreach (var row in chunk)
            {
                var cmd = new NpgsqlBatchCommand(@"
                    INSERT INTO geographies (stfips, areatype, areatypeversion, area, areaname, areatypetitle)
                    VALUES (@stfips, @areatype, @areatypeversion, @area, @areaname, @areatypetitle)
                    ON CONFLICT (stfips, areatype, areatypeversion, area)
                    DO UPDATE SET
                        areaname = EXCLUDED.areaname,
                        areatypetitle = COALESCE(EXCLUDED.areatypetitle, geographies.areatypetitle)");

                cmd.Parameters.AddWithValue("stfips", row.StFips);
                cmd.Parameters.AddWithValue("areatype", row.AreaType);
                cmd.Parameters.AddWithValue("areatypeversion", row.AreaTypeVersion);
                cmd.Parameters.AddWithValue("area", row.Area);
                cmd.Parameters.AddWithValue("areaname", row.AreaTitle);
                cmd.Parameters.AddWithValue("areatypetitle", (object?)row.AreaTypeTitle ?? DBNull.Value);
                batch.BatchCommands.Add(cmd);
            }

            affected += await batch.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }

        return affected;
    }

    private static async Task<int> UpsertCesCodesAsync(
        NpgsqlConnection conn,
        IEnumerable<(string StFips, string SeriesCodeType, string SeriesCode, string SeriesTitle)> rows,
        CancellationToken cancellationToken)
    {
        var affected = 0;
        foreach (var chunk in rows.Chunk(500))
        {
            await using var tx = await conn.BeginTransactionAsync(cancellationToken);
            await using var batch = new NpgsqlBatch(conn, tx);

            foreach (var row in chunk)
            {
                var cmd = new NpgsqlBatchCommand(@"
                    INSERT INTO cescodes (stfips, seriescodetype, seriescode, seriestitle)
                    VALUES (@stfips, @seriescodetype, @seriescode, @seriestitle)
                    ON CONFLICT (stfips, seriescodetype, seriescode)
                    DO UPDATE SET
                        seriestitle = EXCLUDED.seriestitle");

                cmd.Parameters.AddWithValue("stfips", row.StFips);
                cmd.Parameters.AddWithValue("seriescodetype", row.SeriesCodeType);
                cmd.Parameters.AddWithValue("seriescode", row.SeriesCode);
                cmd.Parameters.AddWithValue("seriestitle", row.SeriesTitle);
                batch.BatchCommands.Add(cmd);
            }

            affected += await batch.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }

        return affected;
    }

    private static async Task<int> UpsertIndDirectoriesAsync(
        NpgsqlConnection conn,
        IEnumerable<(string StFips, string ProjPeriod, string IndCodeType, string IndCode, string IndTitle)> rows,
        CancellationToken cancellationToken)
    {
        var affected = 0;
        foreach (var chunk in rows.Chunk(500))
        {
            await using var tx = await conn.BeginTransactionAsync(cancellationToken);
            await using var batch = new NpgsqlBatch(conn, tx);

            foreach (var row in chunk)
            {
                var cmd = new NpgsqlBatchCommand(@"
                    INSERT INTO inddirectories (stfips, projperiod, indindcodetype, indcode, indtitle)
                    VALUES (@stfips, @projperiod, @indcodetype, @indcode, @indtitle)
                    ON CONFLICT (stfips, projperiod, indcodetype, indcode)
                    DO UPDATE SET
                        indtitle = EXCLUDED.indtitle");

                cmd.Parameters.AddWithValue("stfips", row.StFips);
                cmd.Parameters.AddWithValue("projperiod", row.ProjPeriod);
                cmd.Parameters.AddWithValue("indcodetype", row.IndCodeType);
                cmd.Parameters.AddWithValue("indcode", row.IndCode);
                cmd.Parameters.AddWithValue("indtitle", row.IndTitle);
                batch.BatchCommands.Add(cmd);
            }

            affected += await batch.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }

        return affected;
    }

    private static async Task<int> UpsertOccDirectoriesAsync(
        NpgsqlConnection conn,
        IEnumerable<(string StFips, string ProjPeriod, string OccCodeType, string OccCode, string OccTitle)> rows,
        CancellationToken cancellationToken)
    {
        var affected = 0;
        foreach (var chunk in rows.Chunk(500))
        {
            await using var tx = await conn.BeginTransactionAsync(cancellationToken);
            await using var batch = new NpgsqlBatch(conn, tx);

            foreach (var row in chunk)
            {
                var cmd = new NpgsqlBatchCommand(@"
                    INSERT INTO occdirectories (stfips, projperiod, occcodetype, occcode, occtitle)
                    VALUES (@stfips, @projperiod, @occcodetype, @occcode, @occtitle)
                    ON CONFLICT (stfips, projperiod, occcodetype, occcode)
                    DO UPDATE SET
                        occtitle = EXCLUDED.occtitle");

                cmd.Parameters.AddWithValue("stfips", row.StFips);
                cmd.Parameters.AddWithValue("projperiod", row.ProjPeriod);
                cmd.Parameters.AddWithValue("occcodetype", row.OccCodeType);
                cmd.Parameters.AddWithValue("occcode", row.OccCode);
                cmd.Parameters.AddWithValue("occtitle", row.OccTitle);
                batch.BatchCommands.Add(cmd);
            }

            affected += await batch.ExecuteNonQueryAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }

        return affected;
    }
}





