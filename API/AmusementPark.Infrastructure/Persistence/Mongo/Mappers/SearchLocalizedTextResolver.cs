using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class SearchLocalizedTextResolver
{
    public static List<LocalizedTextDocument> Normalize(IEnumerable<LocalizedTextDocument>? values)
    {
        if (values is null)
        {
            return new List<LocalizedTextDocument>();
        }

        Dictionary<string, LocalizedTextDocument> normalizedValues = new Dictionary<string, LocalizedTextDocument>(StringComparer.OrdinalIgnoreCase);

        foreach (LocalizedTextDocument value in values)
        {
            if (string.IsNullOrWhiteSpace(value.LanguageCode) || string.IsNullOrWhiteSpace(value.Value))
            {
                continue;
            }

            string languageCode = NormalizeLanguageCode(value.LanguageCode);
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                continue;
            }

            normalizedValues[languageCode] = new LocalizedTextDocument
            {
                LanguageCode = languageCode,
                Value = value.Value.Trim(),
            };
        }

        return normalizedValues.Values.ToList();
    }

    public static string? Resolve(IEnumerable<LocalizedTextDocument>? values, string? languageCode, string defaultLanguageCode = "en")
    {
        if (values is null)
        {
            return null;
        }

        string normalizedLanguageCode = NormalizeLanguageCode(languageCode);
        if (string.IsNullOrWhiteSpace(normalizedLanguageCode))
        {
            normalizedLanguageCode = NormalizeLanguageCode(defaultLanguageCode);
        }

        string normalizedDefaultLanguageCode = NormalizeLanguageCode(defaultLanguageCode);
        List<LocalizedTextDocument> safeValues = Normalize(values);

        LocalizedTextDocument? exact = safeValues.FirstOrDefault(value =>
            string.Equals(value.LanguageCode, normalizedLanguageCode, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(value.Value));
        if (exact is not null)
        {
            return exact.Value;
        }

        LocalizedTextDocument? fallback = safeValues.FirstOrDefault(value =>
            string.Equals(value.LanguageCode, normalizedDefaultLanguageCode, StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(value.Value));
        if (fallback is not null)
        {
            return fallback.Value;
        }

        LocalizedTextDocument? first = safeValues.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value.Value));
        return first?.Value;
    }

    public static string NormalizeLanguageCode(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return string.Empty;
        }

        string normalized = languageCode.Trim().ToLowerInvariant();
        int dashIndex = normalized.IndexOf('-', StringComparison.Ordinal);
        int underscoreIndex = normalized.IndexOf('_', StringComparison.Ordinal);
        int separatorIndex = ResolveSeparatorIndex(dashIndex, underscoreIndex);

        return separatorIndex > 0
            ? normalized[..separatorIndex]
            : normalized;
    }

    private static int ResolveSeparatorIndex(int first, int second)
    {
        if (first >= 0 && second >= 0)
        {
            return Math.Min(first, second);
        }

        if (first >= 0)
        {
            return first;
        }

        return second;
    }
}
