using System.Text;
using System.Text.Json;

namespace NationalWid.Api.Services;

/// <summary>
/// Encodes/decodes opaque pagination cursors. A cursor is a base64url-encoded JSON
/// payload of <c>{ "o": offset, "s": pageSize }</c> over a stable primary-key ordering.
/// </summary>
public static class CursorCodec
{
    private sealed record CursorPayload(int O, int S);

    public static string Encode(int offset, int pageSize)
    {
        var json = JsonSerializer.Serialize(new CursorPayload(offset, pageSize));
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public static bool TryDecode(string? cursor, out int offset, out int pageSize)
    {
        offset = 0;
        pageSize = 0;
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return false;
        }

        try
        {
            var base64 = cursor.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
            var payload = JsonSerializer.Deserialize<CursorPayload>(json);
            if (payload is null || payload.O < 0 || payload.S <= 0)
            {
                return false;
            }

            offset = payload.O;
            pageSize = payload.S;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
