using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
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

    public HistoryTimelinesSitemapSectionProvider(
        IHistoryEventRepository historyEventRepository,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository)
    {
        this.historyEventRepository = historyEventRepository;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
    }

    public string Key => SitemapSectionKeys.History;

    public string FileName => "history.xml";

    public string DisplayName => "Histoires";

    public async Task<IReadOnlyCollection<SitemapUrlEntry>> GetUrlsAsync(SitemapGenerationContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        IReadOnlyCollection<HistoryEvent> events = await this.historyEventRepository.GetPublicVisibleEventsAsync(PublicHistoryEventLimit, cancellationToken);
        HistorySitemapResolvedData resolvedData = await HistorySitemapCandidateResolver.ResolveAsync(
            events,
            context.SupportedLanguages,
            this.parkRepository,
            this.parkItemRepository,
            cancellationToken);

        Dictionary<string, SitemapUrlEntry> urlsByPath = new Dictionary<string, SitemapUrlEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (HistoryEvent historyEvent in resolvedData.Events)
        {
            if (historyEvent.EntityType == HistoryEntityType.Park)
            {
                AddParkTimelineUrls(urlsByPath, resolvedData, historyEvent);
                continue;
            }

            AddParkItemTimelineUrls(urlsByPath, resolvedData, historyEvent);
        }

        return urlsByPath.Values.OrderBy(static url => url.RelativePath, StringComparer.OrdinalIgnoreCase).ToList();
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
    IReadOnlyDictionary<string, ParkItem> PublicItemById);

internal static class HistorySitemapCandidateResolver
{
    public static async Task<HistorySitemapResolvedData> ResolveAsync(
        IReadOnlyCollection<HistoryEvent> events,
        IReadOnlyCollection<string> supportedLanguages,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> languages = ParksSitemapSectionProvider.NormalizeLanguages(supportedLanguages);
        if (events.Count == 0)
        {
            return new HistorySitemapResolvedData(
                languages,
                Array.Empty<HistoryEvent>(),
                new Dictionary<string, Park>(StringComparer.OrdinalIgnoreCase),
                new Dictionary<string, ParkItem>(StringComparer.OrdinalIgnoreCase));
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

        HashSet<string> parkIds = ResolveParkIds(events, publicItemById);
        IReadOnlyCollection<Park> parks = await parkRepository.GetByIdsAsync(parkIds, cancellationToken);
        Dictionary<string, Park> publicParkById = parks
            .Where(IsPublicHistoryPark)
            .ToDictionary(static park => park.Id!, static park => park, StringComparer.OrdinalIgnoreCase);

        return new HistorySitemapResolvedData(languages, events, publicParkById, publicItemById);
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

            AddIfNotBlank(parkIds, historyEvent.ContextParkId);
            AddIfNotBlank(parkIds, historyEvent.ParkId);

            if (publicItemById.TryGetValue(historyEvent.OwnerId, out ParkItem? item))
            {
                AddIfNotBlank(parkIds, item.ParkId);
            }
        }

        return parkIds;
    }

    private static bool IsPublicHistoryPark(Park park)
    {
        return !string.IsNullOrWhiteSpace(park.Id) &&
               !string.IsNullOrWhiteSpace(park.Name) &&
               park.IsVisible &&
               park.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    private static bool IsPublicHistoryItem(ParkItem item)
    {
        return !string.IsNullOrWhiteSpace(item.Id) &&
               !string.IsNullOrWhiteSpace(item.ParkId) &&
               !string.IsNullOrWhiteSpace(item.Name) &&
               item.IsVisible &&
               item.AdminReviewStatus != AdminReviewStatus.NotRelevant;
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
