using System.Xml.Linq;

namespace AmusementPark.Infrastructure.Services.DataSources.Acquisition;

/// <summary>
/// Lit un sitemap XML générique et retourne ses URLs.
/// </summary>
internal interface IXmlSitemapUrlDiscoveryService
{
    IReadOnlyCollection<string> ReadUrls(string sitemapXmlContent);
}
