using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Seo.Services;

public sealed class HistoryArticlesSitemapSectionProvider : ISitemapSectionProvider
{
    private const int PublicHistoryArticleLimit = 50000;

    private readonly IHistoryEventRepository historyEventRepository;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;

    public HistoryArticlesSitemapSectionProvider(
        IHistoryEventRepository historyEventRepository,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository)
    {
        this.historyEventRepository = historyEventRepository;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
    }

    public string Key => SitemapSectionKeys.HistoryArticles;

    public string FileName => "history-articles.xml";

    public string DisplayName => "Articles d'histoire";

    public async Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyCollection<HistoryEvent> events = await this.historyEventRepository.GetPublicSitemapCandidatesAsync(PublicHistoryArticleLimit, cancellationToken);
        HistorySitemapResolvedData resolvedData = await HistorySitemapCandidateResolver.ResolveAsync(
            events,
            context.SupportedLanguages,
            this.parkRepository,
            this.parkItemRepository,
            cancellationToken);

        Dictionary<string, SitemapUrlEntry> urlsByPath = new Dictionary<string, SitemapUrlEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (HistoryEvent historyEvent in resolvedData.Events.Where(HistorySitemapCandidateResolver.IsPublicArticleEvent))
        {
            if (historyEvent.EntityType == HistoryEntityType.Park)
            {
                AddParkArticleUrls(urlsByPath, resolvedData, historyEvent);
                continue;
            }

            AddParkItemArticleUrls(urlsByPath, resolvedData, historyEvent);
        }

        return urlsByPath.Values.OrderBy(static url => url.RelativePath, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void AddParkArticleUrls(
        Dictionary<string, SitemapUrlEntry> urlsByPath,
        HistorySitemapResolvedData resolvedData,
        HistoryEvent historyEvent)
    {
        if (string.IsNullOrWhiteSpace(historyEvent.Id) ||
            !resolvedData.PublicParkById.TryGetValue(historyEvent.OwnerId, out Park? park))
        {
            return;
        }

        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        string eventSlug = HistorySitemapCandidateResolver.ResolveHistoryEventSlug(historyEvent);
        string pathWithoutLanguage = $"park/{park.Id}/{parkSlug}/history/{historyEvent.Id}/{eventSlug}";
        foreach (string language in resolvedData.Languages)
        {
            HistorySitemapCandidateResolver.AddOrRefreshUrl(
                urlsByPath,
                $"/{language}/{pathWithoutLanguage}",
                historyEvent.UpdatedAtUtc,
                "monthly",
                0.68m);
        }
    }

    private static void AddParkItemArticleUrls(
        Dictionary<string, SitemapUrlEntry> urlsByPath,
        HistorySitemapResolvedData resolvedData,
        HistoryEvent historyEvent)
    {
        if (string.IsNullOrWhiteSpace(historyEvent.Id) ||
            !resolvedData.PublicItemById.TryGetValue(historyEvent.OwnerId, out ParkItem? item))
        {
            return;
        }

        string? articleParkId = HistorySitemapCandidateResolver.NormalizeId(historyEvent.ContextParkId)
            ?? HistorySitemapCandidateResolver.NormalizeId(historyEvent.ParkId)
            ?? item.ParkId;

        if (string.IsNullOrWhiteSpace(articleParkId) ||
            !resolvedData.PublicParkById.TryGetValue(articleParkId, out Park? articlePark))
        {
            return;
        }

        string articleParkSlug = SeoSlugService.ToSlug(articlePark.Name, "park");
        string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
        string eventSlug = HistorySitemapCandidateResolver.ResolveHistoryEventSlug(historyEvent);
        string pathWithoutLanguage = $"park/{articlePark.Id}/{articleParkSlug}/item/{item.Id}/{itemSlug}/history/{historyEvent.Id}/{eventSlug}";
        foreach (string language in resolvedData.Languages)
        {
            HistorySitemapCandidateResolver.AddOrRefreshUrl(
                urlsByPath,
                $"/{language}/{pathWithoutLanguage}",
                historyEvent.UpdatedAtUtc,
                "monthly",
                0.66m);
        }
    }
}
