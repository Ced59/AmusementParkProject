using Microsoft.Extensions.Configuration;

namespace AmusementPark.Infrastructure.Configuration.Trips;

public sealed class TripFingerprintKeyRingSettings
{
    public const string SectionName = "Trips:FingerprintKeyRing";
    public const int MinimumKeyBytes = 32;
    public const int MaximumRetainedKeys = 32;

    public string CurrentVersion { get; set; } = string.Empty;

    public string CurrentKey { get; set; } = string.Empty;

    public string PreviousKeys { get; set; } = string.Empty;

    public static TripFingerprintKeyRingSettings Bind(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        TripFingerprintKeyRingSettings settings = new();
        configuration.GetSection(SectionName).Bind(settings);
        return settings;
    }

    public IReadOnlyDictionary<string, string> GetValidatedKeys()
    {
        string currentVersion = NormalizeVersion(this.CurrentVersion);
        Dictionary<string, string> keys = new(StringComparer.Ordinal)
        {
            [currentVersion] = NormalizeKey(this.CurrentKey, nameof(this.CurrentKey)),
        };

        string[] previousEntries = (this.PreviousKeys ?? string.Empty).Split(
            ';',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string entry in previousEntries)
        {
            int separatorIndex = entry.IndexOf('=');
            if (separatorIndex <= 0 || separatorIndex == entry.Length - 1)
            {
                throw new InvalidOperationException(
                    "Each previous trip fingerprint key must use the version=base64-key format.");
            }

            string version = NormalizeVersion(entry[..separatorIndex]);
            string key = NormalizeKey(entry[(separatorIndex + 1)..], nameof(this.PreviousKeys));
            if (!keys.TryAdd(version, key))
            {
                throw new InvalidOperationException(
                    $"The trip fingerprint key version '{version}' is configured more than once.");
            }
        }

        if (keys.Count > MaximumRetainedKeys)
        {
            throw new InvalidOperationException(
                $"At most {MaximumRetainedKeys} trip fingerprint keys can be retained.");
        }

        return keys;
    }

    private static string NormalizeVersion(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is 0 or > 32
            || normalized.Any(static character => !char.IsAsciiLetterOrDigit(character)
                && character is not '-' and not '_' and not '.'))
        {
            throw new InvalidOperationException(
                "The current trip fingerprint key version must be a short opaque identifier.");
        }

        return normalized;
    }

    private static string NormalizeKey(string? value, string settingName)
    {
        string normalized = value?.Trim() ?? string.Empty;
        byte[]? keyBytes = null;
        try
        {
            keyBytes = Convert.FromBase64String(normalized);
            if (keyBytes.Length < MinimumKeyBytes)
            {
                throw new InvalidOperationException(
                    $"{settingName} must contain at least {MinimumKeyBytes} random bytes.");
            }

            return normalized;
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException($"{settingName} must be valid Base64.", exception);
        }
        finally
        {
            if (keyBytes is not null)
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(keyBytes);
            }
        }
    }
}
