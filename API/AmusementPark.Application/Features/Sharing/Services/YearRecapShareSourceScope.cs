using System.Text;

namespace AmusementPark.Application.Features.Sharing.Services;

public static class YearRecapShareSourceScope
{
    private const string Prefix = "year-recap:";

    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

    public static string Create(string ownerUserId, int year)
    {
        return string.Concat(
            Prefix,
            Encode(ownerUserId?.Trim() ?? string.Empty),
            ":",
            year.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public static bool TryParse(
        string? sourceScopeKey,
        out string ownerUserId,
        out int year)
    {
        ownerUserId = string.Empty;
        year = 0;
        string normalized = sourceScopeKey?.Trim() ?? string.Empty;
        if (!normalized.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        string[] parts = normalized[Prefix.Length..].Split(':');
        if (parts.Length != 2
            || !TryDecode(parts[0], out ownerUserId)
            || ownerUserId.Length == 0
            || !int.TryParse(
                parts[1],
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out year)
            || year < DateOnly.MinValue.Year
            || year > DateOnly.MaxValue.Year)
        {
            ownerUserId = string.Empty;
            year = 0;
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
