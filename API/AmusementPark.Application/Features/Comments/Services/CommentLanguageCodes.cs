namespace AmusementPark.Application.Features.Comments.Services;

internal static class CommentLanguageCodes
{
    public const string DefaultLanguageCode = "en";

    private static readonly HashSet<string> SupportedLanguages = new HashSet<string>(
        new[] { "fr", "en", "de", "nl", "it", "es", "pl", "pt" },
        StringComparer.OrdinalIgnoreCase);

    public static bool IsSupported(string languageCode)
    {
        return SupportedLanguages.Contains(languageCode);
    }

    public static bool TryNormalizeQueryLanguage(
        string? languageCode,
        out string normalizedLanguageCode)
    {
        string candidate = string.IsNullOrWhiteSpace(languageCode)
            ? DefaultLanguageCode
            : languageCode.Trim().ToLowerInvariant().Replace('_', '-');
        int separatorIndex = candidate.IndexOf('-', StringComparison.Ordinal);
        normalizedLanguageCode = separatorIndex > 0
            ? candidate[..separatorIndex]
            : candidate;

        return IsSupported(normalizedLanguageCode);
    }
}
