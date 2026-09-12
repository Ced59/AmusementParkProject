using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Seo.Commands;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.Seo.Queries;
using AmusementPark.Application.Features.Seo.Results;
using AmusementPark.Application.Features.Seo.Services;

namespace AmusementPark.Application.Features.Seo.Handlers;

public sealed class GetSeoSitemapOverviewQueryHandler : IQueryHandler<GetSeoSitemapOverviewQuery, ApplicationResult<SeoSitemapOverviewResult>>
{
    private readonly ISeoSitemapSnapshotRepository snapshotRepository;
    private readonly ISeoSitemapSettingsRepository settingsRepository;
    private readonly ISeoSitemapRuntimeStateStore runtimeStateStore;

    public GetSeoSitemapOverviewQueryHandler(
        ISeoSitemapSnapshotRepository snapshotRepository,
        ISeoSitemapSettingsRepository settingsRepository,
        ISeoSitemapRuntimeStateStore runtimeStateStore)
    {
        this.snapshotRepository = snapshotRepository;
        this.settingsRepository = settingsRepository;
        this.runtimeStateStore = runtimeStateStore;
    }

    public async Task<ApplicationResult<SeoSitemapOverviewResult>> HandleAsync(GetSeoSitemapOverviewQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        SitemapSnapshot? snapshot = await this.snapshotRepository.GetLatestAsync(cancellationToken);
        SeoSitemapSettings settings = await this.settingsRepository.GetAsync(cancellationToken);
        string publicBaseUrl = SitemapXmlWriter.NormalizePublicBaseUrl(query.PublicBaseUrl);
        IReadOnlyCollection<SitemapSectionStats> sections = snapshot?.Sections ?? Array.Empty<SitemapSectionStats>();

        SeoSitemapOverviewResult result = new SeoSitemapOverviewResult
        {
            Runtime = this.runtimeStateStore.GetCurrent(),
            Snapshot = snapshot,
            Settings = settings,
            Sections = sections,
            TotalUrlCount = snapshot?.TotalUrlCount ?? 0,
            SitemapIndexUrl = $"{publicBaseUrl}/sitemap.xml",
            RobotsUrl = $"{publicBaseUrl}/robots.txt",
            IndexNowKeyFileUrl = BuildKeyFileUrl(publicBaseUrl, settings),
            PublicSitemapUrls = sections.Select(section => $"{publicBaseUrl}/{section.FileName}").ToList(),
        };

        return ApplicationResult<SeoSitemapOverviewResult>.Success(result);
    }

    private static string BuildKeyFileUrl(string publicBaseUrl, SeoSitemapSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.IndexNowKey))
        {
            return string.Empty;
        }

        string keyLocation = string.IsNullOrWhiteSpace(settings.IndexNowKeyLocation)
            ? $"/{settings.IndexNowKey}.txt"
            : settings.IndexNowKeyLocation.Trim();
        string normalizedLocation = keyLocation.StartsWith('/') ? keyLocation : $"/{keyLocation}";
        return $"{publicBaseUrl}{normalizedLocation}";
    }
}
