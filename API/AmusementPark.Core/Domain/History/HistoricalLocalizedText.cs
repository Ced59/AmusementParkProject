namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalLocalizedText
{
    public HistoricalLocalizedText(string languageCode, string value)
    {
        string normalizedLanguageCode = languageCode?.Trim().ToLowerInvariant() ?? string.Empty;
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (!HistoricalLocalizationPolicy.IsSupported(normalizedLanguageCode)
            || normalizedValue.Length == 0
            || normalizedValue.Length > 2000)
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidText,
                "A localized historical text requires a supported language and a non-empty value.");
        }

        this.LanguageCode = normalizedLanguageCode;
        this.Value = normalizedValue;
    }

    public string LanguageCode { get; }

    public string Value { get; }
}
