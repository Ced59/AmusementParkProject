namespace AmusementPark.WebAPI.OutputCaching;

/// <summary>
/// Noms stables des policies de cache HTTP de l'API.
/// </summary>
public static class ApiOutputCachePolicyNames
{
    public const string PublicSeoDocuments = "public-seo-documents";
    public const string PublicHtmlSitemapNodes = "public-html-sitemap-nodes";
    public const string PublicDataShort = "public-data-short";
    public const string PublicDataMedium = "public-data-medium";
    public const string PublicWeatherDataShort = "public-weather-data-short";
    public const string PublicReferenceData = "public-reference-data";

    public const string PublicSeoTag = "public-seo";
    public const string PublicDataTag = "public-data";
    public const string PublicWeatherDataTag = "public-weather-data";
    public const string PublicReferenceDataTag = "public-reference-data";
}
