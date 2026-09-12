namespace AmusementPark.Application.Features.Seo.Models;

/// <summary>
/// Contenu persistant du dernier sitemap généré.
/// </summary>
public sealed class SitemapSnapshot
{
    public string Id { get; init; } = "current";

    public DateTime GeneratedAtUtc { get; init; }

    public string PublicBaseUrl { get; init; } = string.Empty;

    public string IndexXml { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> SectionXmlByKey { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<SitemapSectionStats> Sections { get; init; } = Array.Empty<SitemapSectionStats>();

    public int TotalUrlCount { get; init; }
}
