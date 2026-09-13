using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Seo.Models;

namespace AmusementPark.Application.Features.Seo.Ports;

/// <summary>
/// Persiste les réglages administrables SEO sitemap.
/// </summary>
public interface ISeoSitemapSettingsRepository
{
    Task<SeoSitemapSettings> GetAsync(CancellationToken cancellationToken);

    Task SaveAsync(SeoSitemapSettings settings, CancellationToken cancellationToken);
}
