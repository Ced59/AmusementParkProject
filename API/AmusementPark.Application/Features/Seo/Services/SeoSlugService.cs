using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AmusementPark.Application.Features.Seo.Services;

/// <summary>
/// Génération déterministe des slugs publics utilisés par le sitemap seed.
/// </summary>
public static partial class SeoSlugService
{
    public static string ToSlug(string? value, string fallback = "item")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        string normalized = value.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new StringBuilder(normalized.Length);

        foreach (char character in normalized)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        string withoutDiacritics = builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        string slug = NonSlugCharactersRegex().Replace(withoutDiacritics, "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? fallback : slug;
    }

    [GeneratedRegex(@"[^a-z0-9]+", RegexOptions.Compiled)]
    private static partial Regex NonSlugCharactersRegex();
}
