using System.Globalization;
using AmusementPark.Application.Features.Seo.Models;

namespace AmusementPark.Application.Features.Seo.Handlers;

internal static class PublicHtmlSitemapSnapshotLabels
{
    private static readonly IReadOnlyCollection<string> OrderedSectionKeys = SitemapSectionKeys.All.OrderByDescending(static key => key.Length).ToArray();
    private static readonly IReadOnlyDictionary<string, (string All, string General, string Standalone, string Articles, string Part, string Page)> Copy =
        new Dictionary<string, (string, string, string, string, string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = ("All pages", "General pages", "Standalone attractions", "Articles", "Part", "Page"),
            ["fr"] = ("Toutes les pages", "Pages générales", "Attractions indépendantes", "Articles", "Partie", "Page"),
            ["es"] = ("Todas las páginas", "Páginas generales", "Atracciones independientes", "Artículos", "Parte", "Página"),
            ["de"] = ("Alle Seiten", "Allgemeine Seiten", "Eigenständige Attraktionen", "Artikel", "Teil", "Seite"),
            ["it"] = ("Tutte le pagine", "Pagine generali", "Attrazioni indipendenti", "Articoli", "Parte", "Pagina"),
            ["nl"] = ("Alle pagina’s", "Algemene pagina’s", "Zelfstandige attracties", "Artikelen", "Deel", "Pagina"),
            ["pl"] = ("Wszystkie strony", "Strony ogólne", "Samodzielne atrakcje", "Artykuły", "Część", "Strona"),
            ["pt"] = ("Todas as páginas", "Páginas gerais", "Atrações independentes", "Artigos", "Parte", "Página"),
        };

    internal static string AllPages(string language) => ResolveCopy(language).All;

    internal static string Section(string language, string sectionKey)
    {
        string family = OrderedSectionKeys
            .FirstOrDefault(key => sectionKey == key || sectionKey.StartsWith($"{key}-", StringComparison.Ordinal)) ?? string.Empty;
        string label = family switch
        {
            SitemapSectionKeys.Static => ResolveCopy(language).General,
            SitemapSectionKeys.Parks => Label(language, "parks"),
            SitemapSectionKeys.ParkOpeningHours => $"{Label(language, "parks")} · {Label(language, "openingHours")}",
            SitemapSectionKeys.ParkPricing => $"{Label(language, "parks")} · {Label(language, "pricing")}",
            SitemapSectionKeys.History => Label(language, "history"),
            SitemapSectionKeys.HistoryArticles => ResolveCopy(language).Articles,
            SitemapSectionKeys.ParkImages => $"{Label(language, "parks")} · {Label(language, "images")}",
            SitemapSectionKeys.ParkVideos => $"{Label(language, "parks")} · {Label(language, "videos")}",
            SitemapSectionKeys.ParkItemLists => $"{Label(language, "parks")} · {Label(language, "items")}",
            SitemapSectionKeys.ParkZones => $"{Label(language, "parks")} · {Label(language, "zones")}",
            SitemapSectionKeys.ParkItems => Label(language, "items"),
            SitemapSectionKeys.StandaloneAttractions => ResolveCopy(language).Standalone,
            SitemapSectionKeys.ParkItemImages => $"{Label(language, "items")} · {Label(language, "images")}",
            SitemapSectionKeys.ParkItemVideos => $"{Label(language, "items")} · {Label(language, "videos")}",
            SitemapSectionKeys.References => Label(language, "references"),
            SitemapSectionKeys.TechnicalPages => Label(language, "technical"),
            _ => ResolveCopy(language).General,
        };
        string suffix = sectionKey[(sectionKey.LastIndexOf('-') + 1)..];
        return int.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out int part) && part > 0
            ? $"{label} · {ResolveCopy(language).Part} {part}"
            : label;
    }

    internal static string Link(string language, string relativeUrl, string sectionKey)
    {
        string[] segments = relativeUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);
        string last = segments.LastOrDefault() ?? string.Empty;
        if (segments.Length > 3 && segments[^2] == "page" && int.TryParse(last, out int page))
        {
            return $"{Readable(segments[^4])} · {Label(language, "history")} · {ResolveCopy(language).Page} {page}";
        }

        string? labelKey = last switch
        {
            "map" => "interactiveMap",
            "opening-hours" => "openingHours",
            "methodology" => "ratingMethodology",
            "home" or "parks" or "technical" or "references" or "rankings" or "about" or "contact" or "versions" or "privacy" or "sitemap"
                or "weather" or "pricing" or "images" or "videos" or "zones" or "items" or "history" or "manufacturers" => last,
            _ => null,
        };
        if (labelKey is not null)
        {
            if (segments.Length > 3 && Guid.TryParse(segments[^2], out _))
            {
                return Readable(last);
            }

            return segments.Length > 3
                ? $"{Readable(segments[^2])} · {Label(language, labelKey)}"
                : Label(language, labelKey);
        }

        return Guid.TryParse(last, out _) || last.Length == 0 ? Section(language, sectionKey) : Readable(last);
    }

    private static string Readable(string slug)
    {
        string value = Uri.UnescapeDataString(slug).Replace('-', ' ');
        return value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];
    }

    private static string Label(string language, string key) => GetPublicHtmlSitemapNodesQueryHandlerLabelsExtensions.Label(language, key);

    private static (string All, string General, string Standalone, string Articles, string Part, string Page) ResolveCopy(string language)
        => Copy.TryGetValue(language, out (string All, string General, string Standalone, string Articles, string Part, string Page) copy) ? copy : Copy["en"];
}
