using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoasterScraping;

internal sealed class CaptainCoasterScrapingSettings
{
    public string SitemapUrl { get; init; } = "https://captaincoaster.com/sitemap.xml";

    public string MapPageUrl { get; init; } = "https://captaincoaster.com/fr/map/";

    public string Language { get; init; } = "fr";

    public int DelayBetweenRequestsMs { get; init; } = 1200;

    public int TimeoutSeconds { get; init; } = 30;

    public int MaxRetryCount { get; init; } = 3;

    public int? MaxCoasterCount { get; init; }

    public int SkipCoasterCount { get; init; }

    public bool EnrichParkCoordinates { get; init; } = true;

    public string MapMarkersAttributeName { get; init; } = "data-map-markers-value";

    public string CoasterTitleXPath { get; init; } = "//h1";

    public string CharacteristicsItemXPath { get; init; } = "//div[contains(@class,'list-group-item')]";

    public string CharacteristicLabelXPath { get; init; } = ".//label";

    public string CharacteristicValueXPath { get; init; } = ".//div[contains(@class,'pull-right')]";

    public string TopMetricXPath { get; init; } = "//button[contains(@class,'btn-float-lg')]//div[contains(@class,'text-bold')]";
}

internal static partial class CaptainCoasterScrapingUrlParser
{
    public static CaptainCoasterDiscoveredUrl? TryParse(string url, string language)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        Match match = CoasterUrlRegex().Match(url.Trim());
        if (!match.Success)
        {
            return null;
        }

        string urlLanguage = match.Groups["lang"].Value.Trim().ToLowerInvariant();
        if (!string.Equals(urlLanguage, language, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return new CaptainCoasterDiscoveredUrl
        {
            Url = url.Trim(),
            Language = urlLanguage,
            CaptainCoasterId = match.Groups["id"].Value.Trim(),
            Slug = match.Groups["slug"].Value.Trim(),
        };
    }

    [GeneratedRegex(@"https?://captaincoaster\.com/(?<lang>fr|en|de|es)/coasters/(?<id>\d+)/(?<slug>[A-Za-z0-9\-_%]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex CoasterUrlRegex();
}

internal sealed class CaptainCoasterDiscoveredUrl
{
    public string Url { get; init; } = string.Empty;

    public string Language { get; init; } = "fr";

    public string CaptainCoasterId { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;
}

internal sealed class CaptainCoasterParsedCoaster
{
    public string ExternalId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Slug { get; init; } = string.Empty;

    public string SourceUrl { get; init; } = string.Empty;

    public string? ParkName { get; init; }

    public string? ParkSlug { get; init; }

    public string? CountryRaw { get; init; }

    public string? Manufacturer { get; init; }

    public string? Model { get; init; }

    public string? MaterialType { get; init; }

    public string? SeatingType { get; init; }

    public string? LaunchType { get; init; }

    public string? RestraintType { get; init; }

    public bool? IsLaunched { get; init; }

    public double? HeightInMeters { get; init; }

    public double? LengthInMeters { get; init; }

    public double? SpeedInKmH { get; init; }

    public int? InversionCount { get; init; }

    public string? Status { get; init; }

    public string? OpeningDateText { get; init; }

    public string? ClosingDateText { get; init; }

    public DateTime ScrapedAtUtc { get; init; }

    public IReadOnlyDictionary<string, string> RawAttributes { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

internal sealed class CaptainCoasterDerivedPark
{
    public string ExternalId { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Slug { get; init; }

    public string? SourceUrl { get; init; }

    public string? CountryRaw { get; init; }

    public int CoasterCount { get; init; }

    public IReadOnlyCollection<string> SampleCoasterNames { get; init; } = Array.Empty<string>();

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public string? MapParkId { get; init; }

    public string? CoordinateSourceUrl { get; init; }

    public DateTime ScrapedAtUtc { get; init; }
}

internal sealed class CaptainCoasterParkCoordinate
{
    public string? MapParkId { get; init; }

    public string Name { get; init; } = string.Empty;

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public int? CoasterCount { get; init; }

    public string SourceUrl { get; init; } = string.Empty;

    public DateTime ExtractedAtUtc { get; init; }
}

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
