using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Seo.Models;

namespace AmusementPark.Application.Features.Seo.Ports;

/// <summary>
/// Persiste et relit le dernier snapshot sitemap généré.
/// </summary>
public interface ISeoSitemapSnapshotRepository
{
    Task<SitemapSnapshot?> GetLatestAsync(CancellationToken cancellationToken);

    Task<string?> GetSectionXmlAsync(string sectionKey, CancellationToken cancellationToken);

    Task SaveAsync(SitemapSnapshot snapshot, CancellationToken cancellationToken);
}
