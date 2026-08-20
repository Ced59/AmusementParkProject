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

public sealed class HistoryTimelinesSitemapSectionProvider : ISitemapSectionProvider
{
    private const int PublicHistoryEventLimit = 50000;

    private readonly IHistoryEventRepository historyEventRepository;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IStandaloneAttractionRepository? standaloneAttractionRepository;

    public HistoryTimelinesSitemapSectionProvider(
        IHistoryEventRepository historyEventRepository,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IStandaloneAttractionRepository? standaloneAttractionRepository = null)
    {
        this.historyEventRepository = historyEventRepository;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.standaloneAttractionRepository = standaloneAttractionRepository;
    }

    public string Key => SitemapSectionKeys.History;

    public string FileName => "history.xml";

    public string DisplayName => "Histoires";

    public async Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyCollection<HistoryEvent> events = await this.historyEventRepository.GetPublicVisibleEventsAsync(PublicHistoryEventLimit, cancellationToken);
        IReadOnlyCollection<Park> automaticParkCandidates = await LoadPublicHistoryParksAsync(this.parkRepository, cancellationToken);
        IReadOnlyCollection<ParkItem> automaticItemCandidates = await LoadPublicHistoryItemsAsync(this.parkItemRepository, cancellationToken);
        IReadOnlyCollection<StandaloneAttraction> automaticStandaloneCandidates = this.standaloneAttractionRepository is null
            ? Array.Empty<StandaloneAttraction>()
            : (await this.standaloneAttractionRepository.GetPublicSitemapCandidatesAsync(PublicHistoryEventLimit, cancellationToken))
                .Where(static attraction =>
                    HistorySitemapCandidateResolver.IsPublicHistoryStandaloneAttraction(attraction) &&
                    AutomaticHistoryEventFactory.HasLifecycleDate(attraction))
                .ToList();
        IReadOnlyCollection<HistoryEvent> automaticEvents = AutomaticHistoryEventFactory
            .CreateParkLifecycleEvents(automaticParkCandidates)
            .Concat(AutomaticHistoryEventFactory.CreateParkItemLifecycleEvents(automaticItemCandidates))
            .Concat(automaticStandaloneCandidates.SelectMany(AutomaticHistoryEventFactory.CreateStandaloneAttractionLifecycleEvents))
            .ToList();

        if (automaticEvents.Count > 0)
        {
            events = AutomaticHistoryEventFactory.MergeWithExplicitEvents(events, automaticEvents);
        }

        HistorySitemapResolvedData resolvedData = await HistorySitemapCandidateResolver.ResolveAsync(
            events,
            context.SupportedLanguages,
            this.parkRepository,
            this.parkItemRepository,
            cancellationToken,
            this.standaloneAttractionRepository);
        HistoryTimelineIndexability indexability = HistoryTimelineIndexabilityResolver.Resolve(resolvedData);

        Dictionary<string, SitemapUrlEntry> urlsByPath = new Dictionary<string, SitemapUrlEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (HistoryEvent historyEvent in resolvedData.Events)
        {
            if (historyEvent.EntityType == HistoryEntityType.Park)
            {
                if (indexability.ParkIds.Contains(historyEvent.OwnerId))
                {
                    AddParkTimelineUrls(urlsByPath, resolvedData, historyEvent);
                }

                continue;
            }

            if (historyEvent.EntityType == HistoryEntityType.StandaloneAttraction)
            {
                if (indexability.StandaloneAttractionIds.Contains(historyEvent.OwnerId))
                {
                    AddStandaloneAttractionTimelineUrls(urlsByPath, resolvedData, historyEvent);
                }

                continue;
            }

            string? parentParkId = HistoryTimelineIndexabilityResolver.ResolveParentParkId(resolvedData, historyEvent);
            if (parentParkId is not null && indexability.ParkIds.Contains(parentParkId))
            {
                AddParentParkTimelineUrls(urlsByPath, resolvedData, historyEvent);
            }

            if (indexability.ParkItemIds.Contains(historyEvent.OwnerId))
            {
                AddParkItemTimelineUrls(urlsByPath, resolvedData, historyEvent);
            }
        }

        return urlsByPath.Values.OrderBy(static url => url.RelativePath, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static async Task<IReadOnlyCollection<Park>> LoadPublicHistoryParksAsync(
        IParkRepository parkRepository,
        CancellationToken cancellationToken)
    {
        List<Park> parks = new List<Park>();
        int pageNumber = 1;

        while (true)
        {
            PagedResult<Park> page = await parkRepository.GetPageAsync(
                pageNumber,
                SitemapPublicCandidateLoader.PageSize,
                includeHidden: false,
                isVisible: true,
                adminReviewStatus: null,
                type: null,
                countryCode: null,
                hasValidCoordinates: null,
                closedFilter: ClosedEntityFilter.All,
                cancellationToken);

            parks.AddRange(page.Items.Where(static park =>
                HistorySitemapCandidateResolver.IsPublicHistoryPark(park) &&
                AutomaticHistoryEventFactory.HasLifecycleDate(park)));

            if (page.Items.Count == 0 || page.Page >= page.TotalPages)
            {
                break;
            }

            pageNumber++;
        }

        return parks;
    }

    private static async Task<IReadOnlyCollection<ParkItem>> LoadPublicHistoryItemsAsync(
        IParkItemRepository parkItemRepository,
        CancellationToken cancellationToken)
    {
        List<ParkItem> items = new List<ParkItem>();
        int pageNumber = 1;

        while (true)
        {
            PagedResult<ParkItem> page = await parkItemRepository.GetPageAsync(
                pageNumber,
                SitemapPublicCandidateLoader.PageSize,
                parkId: null,
                search: null,
                includeHidden: false,
                isVisible: true,
                adminReviewStatus: null,
                category: null,
                type: null,
                zoneId: null,
                manufacturerId: null,
                contentBacklogFilter: null,
                cancellationToken: cancellationToken,
                sortField: ParkItemAdminSortField.ParkId);

            items.AddRange(page.Items.Where(static item =>
                HistorySitemapCandidateResolver.IsPublicHistoryItem(item) &&
                AutomaticHistoryEventFactory.HasLifecycleDate(item)));

            if (page.Items.Count == 0 || page.Page >= page.TotalPages)
            {
                break;
            }

            pageNumber++;
        }

        return items;
    }

    private static void AddParkTimelineUrls(
        Dictionary<string, SitemapUrlEntry> urlsByPath,
        HistorySitemapResolvedData resolvedData,
        HistoryEvent historyEvent)
    {
        if (!resolvedData.PublicParkById.TryGetValue(historyEvent.OwnerId, out Park? park))
        {
            return;
        }

        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        string pathWithoutLanguage = $"park/{park.Id}/{parkSlug}/history";
        foreach (string language in resolvedData.Languages)
        {
            HistorySitemapCandidateResolver.AddOrRefreshUrl(
                urlsByPath,
                $"/{language}/{pathWithoutLanguage}",
                historyEvent.UpdatedAtUtc,
                "monthly",
                0.72m);
        }
    }

    private static void AddStandaloneAttractionTimelineUrls(
        Dictionary<string, SitemapUrlEntry> urlsByPath,
        HistorySitemapResolvedData resolvedData,
        HistoryEvent historyEvent)
    {
        if (!resolvedData.PublicStandaloneAttractionById.TryGetValue(historyEvent.OwnerId, out StandaloneAttraction? attraction))
        {
            return;
        }

        string attractionSlug = SeoSlugService.ToSlug(attraction.Name, "attraction");
        string pathWithoutLanguage = $"attraction/{attraction.Id}/{attractionSlug}/history";
        foreach (string language in resolvedData.Languages)
        {
            HistorySitemapCandidateResolver.AddOrRefreshUrl(
                urlsByPath,
                $"/{language}/{pathWithoutLanguage}",
                historyEvent.UpdatedAtUtc,
                "monthly",
                0.70m);
        }
    }

    private static void AddParkItemTimelineUrls(
        Dictionary<string, SitemapUrlEntry> urlsByPath,
        HistorySitemapResolvedData resolvedData,
        HistoryEvent historyEvent)
    {
        if (!resolvedData.PublicItemById.TryGetValue(historyEvent.OwnerId, out ParkItem? item))
        {
            return;
        }

        if (!resolvedData.PublicParkById.TryGetValue(item.ParkId, out Park? itemPark))
        {
            return;
        }

        string parkSlug = SeoSlugService.ToSlug(itemPark.Name, "park");
        string itemSlug = SeoSlugService.ToSlug(item.Name, "item");
        string pathWithoutLanguage = $"park/{itemPark.Id}/{parkSlug}/item/{item.Id}/{itemSlug}/history";
        foreach (string language in resolvedData.Languages)
        {
            HistorySitemapCandidateResolver.AddOrRefreshUrl(
                urlsByPath,
                $"/{language}/{pathWithoutLanguage}",
                historyEvent.UpdatedAtUtc,
                "monthly",
                0.70m);
        }
    }

    private static void AddParentParkTimelineUrls(
        Dictionary<string, SitemapUrlEntry> urlsByPath,
        HistorySitemapResolvedData resolvedData,
        HistoryEvent historyEvent)
    {
        if (!resolvedData.PublicItemById.TryGetValue(historyEvent.OwnerId, out ParkItem? item))
        {
            return;
        }

        string? parkId = HistorySitemapCandidateResolver.NormalizeId(historyEvent.ContextParkId)
            ?? HistorySitemapCandidateResolver.NormalizeId(historyEvent.ParkId)
            ?? item.ParkId;
        if (string.IsNullOrWhiteSpace(parkId) ||
            !resolvedData.PublicParkById.TryGetValue(parkId, out Park? park))
        {
            return;
        }

        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        string pathWithoutLanguage = $"park/{park.Id}/{parkSlug}/history";
        foreach (string language in resolvedData.Languages)
        {
            HistorySitemapCandidateResolver.AddOrRefreshUrl(
                urlsByPath,
                $"/{language}/{pathWithoutLanguage}",
                historyEvent.UpdatedAtUtc,
                "monthly",
                0.71m);
        }
    }

}

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

internal sealed record HistorySitemapResolvedData(
    IReadOnlyCollection<string> Languages,
    IReadOnlyCollection<HistoryEvent> Events,
    IReadOnlyDictionary<string, Park> PublicParkById,
    IReadOnlyDictionary<string, ParkItem> PublicItemById,
    IReadOnlyDictionary<string, StandaloneAttraction> PublicStandaloneAttractionById);

internal static class HistorySitemapCandidateResolver
{
    public static async Task<HistorySitemapResolvedData> ResolveAsync(
        IReadOnlyCollection<HistoryEvent> events,
        IReadOnlyCollection<string> supportedLanguages,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        CancellationToken cancellationToken,
        IStandaloneAttractionRepository? standaloneAttractionRepository = null)
    {
        IReadOnlyCollection<string> languages = ParksSitemapSectionProvider.NormalizeLanguages(supportedLanguages);
        if (events.Count == 0)
        {
            return new HistorySitemapResolvedData(
                languages,
                Array.Empty<HistoryEvent>(),
                new Dictionary<string, Park>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, ParkItem>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, StandaloneAttraction>(StringComparer.OrdinalIgnoreCase));
        }

        IReadOnlyCollection<string> itemIds = events
            .Where(static historyEvent => historyEvent.EntityType == HistoryEntityType.ParkItem && !string.IsNullOrWhiteSpace(historyEvent.OwnerId))
            .Select(static historyEvent => historyEvent.OwnerId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        IReadOnlyCollection<ParkItem> items = await parkItemRepository.GetByIdsAsync(itemIds, cancellationToken);
        Dictionary<string, ParkItem> publicItemById = items
            .Where(IsPublicHistoryItem)
            .ToDictionary(static item => item.Id!, static item => item, StringComparer.OrdinalIgnoreCase);

        IReadOnlyCollection<string> standaloneAttractionIds = events
            .Where(static historyEvent => historyEvent.EntityType == HistoryEntityType.StandaloneAttraction && !string.IsNullOrWhiteSpace(historyEvent.OwnerId))
            .Select(static historyEvent => historyEvent.OwnerId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        IReadOnlyCollection<StandaloneAttraction> standaloneAttractions = standaloneAttractionRepository is null || standaloneAttractionIds.Count == 0
            ? Array.Empty<StandaloneAttraction>()
            : await standaloneAttractionRepository.GetByIdsAsync(standaloneAttractionIds, cancellationToken);
        Dictionary<string, StandaloneAttraction> publicStandaloneAttractionById = standaloneAttractions
            .Where(IsPublicHistoryStandaloneAttraction)
            .ToDictionary(static attraction => attraction.Id!, static attraction => attraction, StringComparer.OrdinalIgnoreCase);

        HashSet<string> parkIds = ResolveParkIds(events, publicItemById);
        IReadOnlyCollection<Park> parks = await parkRepository.GetByIdsAsync(parkIds, cancellationToken);
        Dictionary<string, Park> publicParkById = parks
            .Where(IsPublicHistoryPark)
            .ToDictionary(static park => park.Id!, static park => park, StringComparer.OrdinalIgnoreCase);

        return new HistorySitemapResolvedData(languages, events, publicParkById, publicItemById, publicStandaloneAttractionById);
    }

    public static bool IsPublicArticleEvent(HistoryEvent historyEvent)
    {
        return !string.IsNullOrWhiteSpace(historyEvent.Id) &&
               historyEvent.IsMajor &&
               historyEvent.Article is not null &&
               historyEvent.Article.IsPublished;
    }

    public static string ResolveHistoryEventSlug(HistoryEvent historyEvent)
    {
        string? slugSource = NormalizeId(historyEvent.Article?.Slug)
            ?? NormalizeId(historyEvent.Slug)
            ?? ResolveFirstText(historyEvent.Article?.Titles)
            ?? ResolveFirstText(historyEvent.Titles)
            ?? historyEvent.Key
            ?? historyEvent.Id;

        return SeoSlugService.ToSlug(slugSource, "history");
    }

    public static void AddOrRefreshUrl(
        Dictionary<string, SitemapUrlEntry> urlsByPath,
        string relativePath,
        DateTime? lastModifiedUtc,
        string changeFrequency,
        decimal priority)
    {
        if (!urlsByPath.TryGetValue(relativePath, out SitemapUrlEntry? existingUrl) ||
            IsNewer(lastModifiedUtc, existingUrl.LastModifiedUtc))
        {
            urlsByPath[relativePath] = new SitemapUrlEntry(relativePath, lastModifiedUtc, changeFrequency, priority);
        }
    }

    public static string? NormalizeId(string? value)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(normalizedValue) ? null : normalizedValue;
    }

    private static HashSet<string> ResolveParkIds(
        IReadOnlyCollection<HistoryEvent> events,
        IReadOnlyDictionary<string, ParkItem> publicItemById)
    {
        HashSet<string> parkIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (HistoryEvent historyEvent in events)
        {
            if (historyEvent.EntityType == HistoryEntityType.Park)
            {
                AddIfNotBlank(parkIds, historyEvent.OwnerId);
                AddIfNotBlank(parkIds, historyEvent.ParkId);
                continue;
            }

            if (historyEvent.EntityType == HistoryEntityType.StandaloneAttraction)
            {
                continue;
            }

            AddIfNotBlank(parkIds, historyEvent.ContextParkId);
            AddIfNotBlank(parkIds, historyEvent.ParkId);

            if (publicItemById.TryGetValue(historyEvent.OwnerId, out ParkItem? item))
            {
                AddIfNotBlank(parkIds, item.ParkId);
            }
        }

        return parkIds;
    }

    public static bool IsPublicHistoryPark(Park park)
    {
        return !string.IsNullOrWhiteSpace(park.Id) &&
               !string.IsNullOrWhiteSpace(park.Name) &&
               park.IsVisible &&
               park.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    public static bool IsPublicHistoryItem(ParkItem item)
    {
        return !string.IsNullOrWhiteSpace(item.Id) &&
               !string.IsNullOrWhiteSpace(item.ParkId) &&
               !string.IsNullOrWhiteSpace(item.Name) &&
               item.IsVisible &&
               item.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    public static bool IsPublicHistoryStandaloneAttraction(StandaloneAttraction attraction)
    {
        return attraction.IsPubliclyPublishable();
    }

    private static string? ResolveFirstText(IReadOnlyCollection<LocalizedText>? texts)
    {
        if (texts is null)
        {
            return null;
        }

        LocalizedText? text = texts.FirstOrDefault(static item => !string.IsNullOrWhiteSpace(item.Value));
        return NormalizeId(text?.Value);
    }

    private static void AddIfNotBlank(HashSet<string> ids, string? id)
    {
        string? normalizedId = NormalizeId(id);
        if (normalizedId is null)
        {
            return;
        }

        ids.Add(normalizedId);
    }

    private static bool IsNewer(DateTime? candidate, DateTime? existing)
    {
        if (!candidate.HasValue)
        {
            return false;
        }

        if (!existing.HasValue)
        {
            return true;
        }

        return candidate.Value > existing.Value;
    }
}
