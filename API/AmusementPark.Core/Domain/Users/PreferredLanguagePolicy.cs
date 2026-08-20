namespace AmusementPark.Core.Domain.Users;

/// <summary>
/// Defines the language codes supported as user preferences.
/// </summary>
public static class PreferredLanguagePolicy
{
    private static readonly HashSet<string> SupportedLanguages = new HashSet<string>(
        new[] { "EN", "FR", "ES", "DE", "IT", "PL", "NL", "PT" },
        StringComparer.Ordinal);

    public static bool TryNormalize(string? language, out string normalizedLanguage)
    {
        normalizedLanguage = (language ?? string.Empty).Trim().ToUpperInvariant();
        return SupportedLanguages.Contains(normalizedLanguage);
    }
}
