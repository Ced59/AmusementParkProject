using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Seo.Models;

namespace AmusementPark.Application.Features.Seo.Ports;

/// <summary>
/// Écrit les documents XML sitemap.
/// </summary>
public interface ISitemapXmlWriter
{
    string WriteUrlSet(string publicBaseUrl, IReadOnlyCollection<SitemapUrlEntry> urls);

    string WriteSitemapIndex(string publicBaseUrl, IReadOnlyCollection<SitemapSectionStats> sections);
}
