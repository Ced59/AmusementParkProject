using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.SocialPublishing.Services;

internal static class SocialPublicationLocalizedTextResolver
{
    public static string Resolve(
        IEnumerable<LocalizedText>? values,
        string languageCode,
        string fallback)
    {
        string? value = values?
            .FirstOrDefault(candidate => string.Equals(candidate.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase))
            ?.Value;
        return string.IsNullOrWhiteSpace(value) ? fallback.Trim() : value.Trim();
    }
}
