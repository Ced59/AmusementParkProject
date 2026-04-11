using System;
using System.Collections.Generic;
using System.Linq;
using AmusementPark.Core.Localization;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Mappers;

/// <summary>
/// Helpers de mapping HTTP pour les valeurs localisées.
/// </summary>
internal static class LocalizationHttpMapper
{
    public static List<LocalizedText> ToDomain(this IEnumerable<LocalizedTextDto>? values)
    {
        if (values is null)
        {
            return new List<LocalizedText>();
        }

        List<LocalizedTextDto> normalized = values
            .Where(static value => value is not null)
            .Where(static value => !string.IsNullOrWhiteSpace(value.LanguageCode))
            .Select(static value => new LocalizedTextDto
            {
                LanguageCode = value.LanguageCode.Trim().ToLowerInvariant(),
                Value = value.Value?.Trim(),
            })
            .Where(static value => !string.IsNullOrWhiteSpace(value.Value))
            .ToList();

        Dictionary<string, LocalizedText> deduplicated = new Dictionary<string, LocalizedText>(StringComparer.OrdinalIgnoreCase);

        foreach (LocalizedTextDto value in normalized)
        {
            deduplicated[value.LanguageCode] = new LocalizedText(value.LanguageCode, value.Value);
        }

        return deduplicated.Values.ToList();
    }

    public static List<LocalizedTextDto> ToHttp(this IEnumerable<LocalizedText>? values)
    {
        if (values is null)
        {
            return new List<LocalizedTextDto>();
        }

        return values
            .Select(static value => new LocalizedTextDto
            {
                LanguageCode = value.LanguageCode,
                Value = value.Value,
            })
            .ToList();
    }

    public static string Resolve(this IEnumerable<LocalizedText>? values, string? languageCode, string defaultLanguageCode = "en")
    {
        if (values is null)
        {
            return string.Empty;
        }

        string normalizedDefaultLanguageCode = string.IsNullOrWhiteSpace(defaultLanguageCode)
            ? "en"
            : defaultLanguageCode.Trim().ToLowerInvariant();

        string normalizedLanguageCode = string.IsNullOrWhiteSpace(languageCode)
            ? normalizedDefaultLanguageCode
            : languageCode.Trim().ToLowerInvariant();

        List<LocalizedText> safeValues = values
            .Where(static value => value is not null)
            .Where(static value => !string.IsNullOrWhiteSpace(value.LanguageCode))
            .ToList();

        LocalizedText? exact = safeValues.FirstOrDefault(value => string.Equals(value.LanguageCode, normalizedLanguageCode, StringComparison.OrdinalIgnoreCase));
        if (exact is not null && !string.IsNullOrWhiteSpace(exact.Value))
        {
            return exact.Value;
        }

        LocalizedText? fallback = safeValues.FirstOrDefault(value => string.Equals(value.LanguageCode, normalizedDefaultLanguageCode, StringComparison.OrdinalIgnoreCase));
        if (fallback is not null && !string.IsNullOrWhiteSpace(fallback.Value))
        {
            return fallback.Value;
        }

        LocalizedText? first = safeValues.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value.Value));
        return first?.Value ?? string.Empty;
    }
}
