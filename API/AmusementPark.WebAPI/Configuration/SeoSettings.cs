namespace AmusementPark.WebAPI.Configuration;

/// <summary>
/// Paramètres SEO publics utilisés pour robots.txt et sitemap.xml.
/// </summary>
public sealed class SeoSettings
{
    public const string SectionName = "Seo";

    public string PublicBaseUrl { get; set; } = "https://amusement-parks.fun";

    public string DefaultLanguage { get; set; } = "en";

    public List<string> SupportedLanguages { get; set; } = new()
    {
        "en",
        "fr",
        "es",
        "de",
        "it",
        "pl",
        "nl",
        "pt",
    };

    public int MaxDynamicUrlsPerType { get; set; } = 10000;

    public List<string> RobotsDisallowPaths { get; set; } = new()
    {
        "/api/",
        "/{lang}/admin/",
        "/{lang}/profile",
        "/{lang}/confirm-account",
        "/{lang}/forgot-password",
        "/{lang}/reset-password",
    };

    public string GetNormalizedPublicBaseUrl()
    {
        string value = string.IsNullOrWhiteSpace(this.PublicBaseUrl)
            ? "https://amusement-parks.fun"
            : this.PublicBaseUrl.Trim();

        return value.TrimEnd('/');
    }
}
