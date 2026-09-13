using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Seo.Models;

namespace AmusementPark.Application.Features.Seo.Ports;

/// <summary>
/// Fournit les URLs d'une section de sitemap.
/// </summary>
public interface ISitemapSectionProvider
{
    string Key { get; }

    string FileName { get; }

    string DisplayName { get; }

    Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken);
}
