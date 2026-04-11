using System.Xml.Linq;

namespace AmusementPark.Infrastructure.Services.DataSources.Acquisition;

/// <summary>
/// Lit un sitemap XML générique et retourne ses URLs.
/// </summary>
internal interface IXmlSitemapUrlDiscoveryService
{
    IReadOnlyCollection<string> ReadUrls(string sitemapXmlContent);
}

internal sealed class XmlSitemapUrlDiscoveryService : IXmlSitemapUrlDiscoveryService
{
    public IReadOnlyCollection<string> ReadUrls(string sitemapXmlContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sitemapXmlContent);

        XDocument document = XDocument.Parse(sitemapXmlContent, LoadOptions.None);
        XNamespace sitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

        List<string> urls = document
            .Descendants(sitemapNamespace + "url")
            .Elements(sitemapNamespace + "loc")
            .Select(static element => element.Value?.Trim())
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string>()
            .ToList();

        return urls;
    }
}
