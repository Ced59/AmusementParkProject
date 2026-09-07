using System.Text;

namespace AmusementPark.Application.Features.Sharing.Services;

public static class VisitRecapShareSourceScope
{
    private const string Prefix = "visit-recap:";

    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    public static string Create(string ownerUserId, string visitId)
    {
        return string.Concat(
            Prefix,
            Encode(ownerUserId?.Trim() ?? string.Empty),
            ":",
            Encode(visitId?.Trim() ?? string.Empty));
    }

    public static bool TryParse(
        string? sourceScopeKey,
        out string ownerUserId,
        out string visitId)
    {
        ownerUserId = string.Empty;
        visitId = string.Empty;
        string normalized = sourceScopeKey?.Trim() ?? string.Empty;
        if (!normalized.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        string[] parts = normalized[Prefix.Length..].Split(':');
        if (parts.Length != 2
            || !TryDecode(parts[0], out ownerUserId)
            || !TryDecode(parts[1], out visitId)
            || ownerUserId.Length == 0
            || visitId.Length == 0)
        {
            ownerUserId = string.Empty;
            visitId = string.Empty;
            return false;
        }

        return true;
    }

    private static string Encode(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static bool TryDecode(string value, out string decoded)
    {
        decoded = string.Empty;
        string base64 = value.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');
        try
        {
            decoded = StrictUtf8.GetString(Convert.FromBase64String(base64));
            return string.Equals(Encode(decoded), value, StringComparison.Ordinal);
        }
        catch (Exception exception) when (
            exception is FormatException or DecoderFallbackException)
        {
            return false;
        }
    }
}
