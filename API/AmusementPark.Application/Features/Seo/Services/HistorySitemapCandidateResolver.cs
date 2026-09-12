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
