using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Seo.Models;

namespace AmusementPark.Application.Features.Seo.Ports;

/// <summary>
/// Persiste et recherche l'historique de génération des sitemaps.
/// </summary>
public interface ISeoSitemapGenerationHistoryRepository
{
    Task WriteAsync(SitemapGenerationHistoryEntry entry, CancellationToken cancellationToken);

    Task<PagedResult<SitemapGenerationHistoryEntry>> SearchAsync(PagedQuery paging, CancellationToken cancellationToken);
}
