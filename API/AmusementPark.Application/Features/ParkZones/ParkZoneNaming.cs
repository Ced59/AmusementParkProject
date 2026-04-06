using System.Text.RegularExpressions;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkZones;

/// <summary>
/// Helpers de normalisation métier des zones de parc.
/// </summary>
internal static class ParkZoneNaming
{
    public static void Normalize(ParkZone zone, string? fallbackName = null)
    {
        ArgumentNullException.ThrowIfNull(zone);

        string displayName = ResolveDisplayName(zone.Names, zone.Name, fallbackName);
        zone.Name = displayName;
        zone.Slug = BuildSlug(displayName);
        zone.ParkId = zone.ParkId.Trim();
    }

    private static string ResolveDisplayName(IEnumerable<LocalizedText>? names, params string?[] fallbacks)
    {
        if (names is not null)
        {
            List<LocalizedText> safeNames = names
                .Where(static value => value is not null)
                .Where(static value => !string.IsNullOrWhiteSpace(value.LanguageCode))
                .ToList();

            LocalizedText? english = safeNames.FirstOrDefault(static value =>
                string.Equals(value.LanguageCode, "en", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(value.Value));

            if (english is not null)
            {
                return english.Value!.Trim();
            }

            LocalizedText? first = safeNames.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value.Value));
            if (first is not null)
            {
                return first.Value!.Trim();
            }
        }

        foreach (string? fallback in fallbacks)
        {
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                return fallback.Trim();
            }
        }

        return "zone";
    }

    private static string BuildSlug(string value)
    {
        string normalized = value.Trim().ToLowerInvariant();
        normalized = Regex.Replace(normalized, "[^a-z0-9]+", "-");
        normalized = Regex.Replace(normalized, "-+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "zone" : normalized;
    }
}
