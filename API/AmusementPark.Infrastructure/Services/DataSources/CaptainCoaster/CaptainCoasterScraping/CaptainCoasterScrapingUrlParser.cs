using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace AmusementPark.Infrastructure.Services.DataSources.CaptainCoaster.CaptainCoasterScraping;

internal static class CaptainCoasterScrapingUrlParser
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

    private static readonly Regex CoasterUrlPattern = new Regex(
        @"https?://captaincoaster\.com/(?<lang>fr|en|de|es)/coasters/(?<id>\d+)/(?<slug>[A-Za-z0-9\-_%]+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static Regex CoasterUrlRegex()
    {
        return CoasterUrlPattern;
    }
}
