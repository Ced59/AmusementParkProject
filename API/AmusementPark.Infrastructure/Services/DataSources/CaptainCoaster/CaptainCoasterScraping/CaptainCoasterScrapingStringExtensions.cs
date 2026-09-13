using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;

internal static class CaptainCoasterScrapingStringExtensions
{
    public static string CleanText(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string deEntitized = HtmlEntity.DeEntitize(value);
        string normalized = Regex.Replace(deEntitized, @"\s+", " ", RegexOptions.Compiled);
        return normalized.Trim();
    }

    public static string ToSlugValue(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string normalized = value.Normalize(NormalizationForm.FormD);
        List<char> characters = new List<char>(normalized.Length);
        foreach (char character in normalized)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                characters.Add(character);
            }
        }

        string withoutDiacritics = new string(characters.ToArray()).Normalize(NormalizationForm.FormC).ToLowerInvariant();
        string replaced = Regex.Replace(withoutDiacritics, @"[^a-z0-9]+", "-", RegexOptions.Compiled);
        return replaced.Trim('-');
    }
}
