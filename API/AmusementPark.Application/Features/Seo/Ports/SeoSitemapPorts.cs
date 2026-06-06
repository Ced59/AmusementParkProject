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

/// <summary>
/// Écrit les documents XML sitemap.
/// </summary>
public interface ISitemapXmlWriter
{
    string WriteUrlSet(string publicBaseUrl, IReadOnlyCollection<SitemapUrlEntry> urls);

    string WriteSitemapIndex(string publicBaseUrl, IReadOnlyCollection<SitemapSectionStats> sections);
}

/// <summary>
/// Persiste et relit le dernier snapshot sitemap généré.
/// </summary>
public interface ISeoSitemapSnapshotRepository
{
    Task<SitemapSnapshot?> GetLatestAsync(CancellationToken cancellationToken);

    Task SaveAsync(SitemapSnapshot snapshot, CancellationToken cancellationToken);
}

/// <summary>
/// Persiste et recherche l'historique de génération des sitemaps.
/// </summary>
public interface ISeoSitemapGenerationHistoryRepository
{
    Task WriteAsync(SitemapGenerationHistoryEntry entry, CancellationToken cancellationToken);

    Task<PagedResult<SitemapGenerationHistoryEntry>> SearchAsync(PagedQuery paging, CancellationToken cancellationToken);
}

/// <summary>
/// Persiste les réglages administrables SEO sitemap.
/// </summary>
public interface ISeoSitemapSettingsRepository
{
    Task<SeoSitemapSettings> GetAsync(CancellationToken cancellationToken);

    Task SaveAsync(SeoSitemapSettings settings, CancellationToken cancellationToken);
}

/// <summary>
/// Soumet des URLs à IndexNow ou à un endpoint compatible.
/// </summary>
public interface IIndexNowSubmitter
{
    Task<IndexNowSubmissionResult> SubmitAsync(SeoSitemapSettings settings, string publicBaseUrl, IReadOnlyCollection<string> absoluteUrls, CancellationToken cancellationToken);
}

/// <summary>
/// Expose l'état runtime de génération au panneau admin.
/// </summary>
public interface ISeoSitemapRuntimeStateStore
{
    SitemapRuntimeState GetCurrent();

    bool TryStart(string step);

    void Update(string step, int progressPercentage, string? message = null);

    void Complete(string step, string? message = null);

    void Fail(string step, string message);
}
