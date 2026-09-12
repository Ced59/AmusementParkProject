using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace AmusementPark.Application.Features.Sharing.Services;

public static class PassportProfileShareSourceScope
{
    private const string Prefix = "passport-profile:";
    private const string CoordinationPrefix = "passport-profile-coordination:";
    private const string FingerprintPrefix = "passport-profile-fingerprint:";
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

    public static string CreateCoordination(string ownerUserId)
    {
        string normalizedOwner = ownerUserId?.Trim() ?? string.Empty;
        if (normalizedOwner.Length == 0)
        {
            throw new ArgumentException("A share owner identifier is required.", nameof(ownerUserId));
        }

        return string.Concat(CoordinationPrefix, Encode(normalizedOwner));
    }

    public static string CreateFingerprint(
        string ownerUserId,
        IEnumerable<int> years,
        IEnumerable<string> parkIds)
    {
        string normalizedOwner = ownerUserId?.Trim() ?? string.Empty;
        if (normalizedOwner.Length == 0)
        {
            throw new ArgumentException("A share owner identifier is required.", nameof(ownerUserId));
        }

        ArgumentNullException.ThrowIfNull(years);
        ArgumentNullException.ThrowIfNull(parkIds);
        int[] normalizedYears = years
            .Distinct()
            .OrderBy(static year => year)
            .ToArray();
        string[] normalizedParkIds = parkIds
            .Select(static parkId => parkId?.Trim() ?? string.Empty)
            .Where(static parkId => parkId.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static parkId => parkId, StringComparer.Ordinal)
            .ToArray();
        StringBuilder canonical = new StringBuilder();
        Append(canonical, "years");
        Append(canonical, normalizedYears.Length);
        foreach (int year in normalizedYears)
        {
            Append(canonical, year);
        }

        Append(canonical, "parks");
        Append(canonical, normalizedParkIds.Length);
        foreach (string parkId in normalizedParkIds)
        {
            Append(canonical, parkId);
        }

        string digest = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())))
            .ToLowerInvariant();
        return string.Concat(FingerprintPrefix, Encode(normalizedOwner), ":", digest);
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

    private static void Append(StringBuilder target, object value)
    {
        string text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
        target.Append(text.Length.ToString(CultureInfo.InvariantCulture))
            .Append(':')
            .Append(text);
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
