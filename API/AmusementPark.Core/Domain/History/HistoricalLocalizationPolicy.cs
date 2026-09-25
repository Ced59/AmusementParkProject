namespace AmusementPark.Core.Domain.History;

public static class HistoricalLocalizationPolicy
{
    private static readonly string[] LanguageCodes =
    {
        "fr",
        "en",
        "de",
        "nl",
        "it",
        "es",
        "pl",
        "pt",
    };

    private static readonly IReadOnlyCollection<string> ReadOnlyLanguageCodes =
        Array.AsReadOnly(LanguageCodes);

    public static IReadOnlyCollection<string> SupportedLanguageCodes => ReadOnlyLanguageCodes;

    public static bool IsSupported(string? languageCode)
    {
        return !string.IsNullOrWhiteSpace(languageCode)
            && LanguageCodes.Contains(languageCode.Trim(), StringComparer.OrdinalIgnoreCase);
    }
}
