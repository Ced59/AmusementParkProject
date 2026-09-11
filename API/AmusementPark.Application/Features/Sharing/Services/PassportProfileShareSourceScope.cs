using System.Text;

namespace AmusementPark.Application.Features.Sharing.Services;

public static class PassportProfileShareSourceScope
{
    private const string Prefix = "passport-profile:";
    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    public static string Create(string ownerUserId)
    {
        string normalizedOwner = ownerUserId?.Trim() ?? string.Empty;
        if (normalizedOwner.Length == 0)
        {
            throw new ArgumentException("A share owner identifier is required.", nameof(ownerUserId));
        }

        return string.Concat(Prefix, Encode(normalizedOwner));
    }

    public static bool TryParse(string? sourceScopeKey, out string ownerUserId)
    {
        ownerUserId = string.Empty;
        string normalized = sourceScopeKey?.Trim() ?? string.Empty;
        return normalized.StartsWith(Prefix, StringComparison.Ordinal)
            && TryDecode(normalized[Prefix.Length..], out ownerUserId)
            && ownerUserId.Length > 0;
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
