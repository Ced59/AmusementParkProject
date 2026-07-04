using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Seo.Handlers;

public sealed partial class GetPublicHtmlSitemapNodesQueryHandler
{
    private async Task<Park?> GetPublicParkAsync(string parkId, CancellationToken cancellationToken)
    {
        Park? park = await this.parkRepository.GetByIdAsync(parkId, includeHidden: false, cancellationToken);
        return park is not null && ParksSitemapSectionProvider.IsPublicPark(park) ? park : null;
    }

    private async Task<IReadOnlyCollection<ParkItem>> GetPublicParkItemsAsync(
        string parkId,
        CancellationToken cancellationToken)
    {
        List<ParkItem> items = new List<ParkItem>();
        int pageNumber = 1;

        while (true)
        {
            PagedResult<ParkItem> page = await this.parkItemRepository.GetPublicPageByParkIdAsync(
                pageNumber,
                PublicListPageSize,
                parkId,
                search: null,
                includeHidden: false,
                ClosedEntityFilter.OpenOnly,
                category: null,
                type: null,
                zoneId: null,
                manufacturerIds: Array.Empty<string>(),
                cancellationToken);

            items.AddRange(page.Items.Where(ParkItemsSitemapSectionProvider.IsPublicItem));

            if (page.Items.Count == 0 || page.Page >= page.TotalPages)
            {
                break;
            }

            pageNumber++;
        }

        return items;
    }

    private async Task<IReadOnlyCollection<ParkItem>> GetPublicHistoryParkItemsAsync(
        string parkId,
        CancellationToken cancellationToken)
    {
        List<ParkItem> items = new List<ParkItem>();
        int pageNumber = 1;

        while (true)
        {
            PagedResult<ParkItem> page = await this.parkItemRepository.GetPublicPageByParkIdAsync(
                pageNumber,
                PublicListPageSize,
                parkId,
                search: null,
                includeHidden: false,
                ClosedEntityFilter.All,
                category: null,
                type: null,
                zoneId: null,
                manufacturerIds: Array.Empty<string>(),
                cancellationToken);

            items.AddRange(page.Items.Where(HistorySitemapCandidateResolver.IsPublicHistoryItem));

            if (page.Items.Count == 0 || page.Page >= page.TotalPages)
            {
                break;
            }

            pageNumber++;
        }

        return items;
    }

    private async Task<bool> HasOpeningHoursAsync(string parkId, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, ParkOpeningHoursScheduleSummary> summaries =
            await this.openingHoursRepository.GetSummariesByParkIdsAsync(new[] { parkId }, cancellationToken);

        return summaries.TryGetValue(parkId, out ParkOpeningHoursScheduleSummary? summary)
               && summary.HasScheduleData;
    }

    private async Task<bool> HasParkImagesAsync(
        string parkId,
        IReadOnlyCollection<ParkItem> publicItems,
        CancellationToken cancellationToken)
    {
        if (await this.HasPublishedImagesAsync(ImageOwnerType.Park, ImageCategory.Park, parkId, cancellationToken))
        {
            return true;
        }

        IReadOnlyCollection<string> itemIds = publicItems
            .Where(static item => !string.IsNullOrWhiteSpace(item.Id))
            .Select(static item => item.Id!)
            .ToList();
        return await this.HasPublishedImagesAsync(ImageOwnerType.ParkItem, ImageCategory.ParkItem, itemIds, cancellationToken);
    }

    private async Task<bool> HasPublishedImagesAsync(
        ImageOwnerType ownerType,
        ImageCategory category,
        string ownerId,
        CancellationToken cancellationToken)
    {
        return await this.HasPublishedImagesAsync(ownerType, category, new[] { ownerId }, cancellationToken);
    }

    private async Task<bool> HasPublishedImagesAsync(
        ImageOwnerType ownerType,
        ImageCategory category,
        IReadOnlyCollection<string> ownerIds,
        CancellationToken cancellationToken)
    {
        if (ownerIds.Count == 0)
        {
            return false;
        }

        ImageSearchCriteria criteria = new ImageSearchCriteria(
            Category: category,
            OwnerType: ownerType,
            IsPublished: true,
            HasOwner: true,
            OwnerIds: ownerIds);
        PagedResult<Image> page = await this.imageRepository.GetPageAsync(1, 1, criteria, cancellationToken);
        return page.Items.Count > 0;
    }

    private async Task<bool> HasVideosAsync(
        VideoOwnerType ownerType,
        string ownerId,
        string language,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Video> videos = await this.GetVideosAsync(ownerType, ownerId, language, cancellationToken);
        return videos.Count > 0;
    }

    private async Task<IReadOnlyCollection<Video>> GetVideosAsync(
        VideoOwnerType ownerType,
        string ownerId,
        string language,
        CancellationToken cancellationToken)
    {
        List<Video> videos = new List<Video>();
        int pageNumber = 1;

        while (true)
        {
            VideoSearchCriteria criteria = new VideoSearchCriteria(
                OwnerType: ownerType,
                OwnerId: ownerId,
                IsPublished: true,
                SortBy: "updated",
                SortDirection: "desc");
            PagedResult<Video> page = await this.videoRepository.GetPageAsync(pageNumber, PublicMediaPageSize, criteria, cancellationToken);

            videos.AddRange(page.Items.Where(video => IsPublicVideo(video, ownerType, ownerId) && VideoSitemapSectionProviderHelpers.IsVisibleInLanguage(video, language)));

            if (page.Items.Count == 0 || page.Page >= page.TotalPages)
            {
                break;
            }

            pageNumber++;
        }

        return videos;
    }

    private async Task<bool> HasVisibleZonesAsync(
        string parkId,
        IReadOnlyCollection<ParkItem> publicItems,
        CancellationToken cancellationToken)
    {
        HashSet<string> visibleZoneIds = publicItems
            .Select(static item => item.ZoneId)
            .Where(static zoneId => !string.IsNullOrWhiteSpace(zoneId))
            .Select(static zoneId => zoneId!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (visibleZoneIds.Count == 0)
        {
            return false;
        }

        IReadOnlyCollection<ParkZone> zones = await this.parkZoneRepository.GetByParkIdAsync(parkId, cancellationToken);
        return zones.Any(zone => ParkZonesSitemapSectionProvider.IsPublicZone(zone) && visibleZoneIds.Contains(zone.Id!));
    }

    private async Task<bool> HasParkHistoryAsync(
        Park park,
        IReadOnlyCollection<ParkItem> publicItems,
        IReadOnlyCollection<string>? itemIdsWithExplicitHistory,
        CancellationToken cancellationToken)
    {
        if (AutomaticHistoryEventFactory.HasLifecycleDate(park) || publicItems.Any(AutomaticHistoryEventFactory.HasLifecycleDate))
        {
            return true;
        }

        if (itemIdsWithExplicitHistory is not null)
        {
            return itemIdsWithExplicitHistory.Count > 0;
        }

        IReadOnlyCollection<string> itemIds = publicItems
            .Where(static item => !string.IsNullOrWhiteSpace(item.Id))
            .Select(static item => item.Id!)
            .ToList();
        IReadOnlyCollection<HistoryEvent> events = await this.historyEventRepository.GetParkTimelineAsync(
            park.Id!,
            includeHidden: false,
            includeParkItemEvents: true,
            itemIds,
            cancellationToken);

        return events.Any(static historyEvent => historyEvent.IsVisible);
    }

    private async Task<bool> HasParkItemHistoryAsync(ParkItem item, CancellationToken cancellationToken)
    {
        if (AutomaticHistoryEventFactory.HasLifecycleDate(item))
        {
            return true;
        }

        IReadOnlyCollection<HistoryEvent> events = await this.historyEventRepository.GetOwnerTimelineAsync(
            HistoryEntityType.ParkItem,
            item.Id!,
            includeHidden: false,
            cancellationToken);

        return events.Any(static historyEvent => historyEvent.IsVisible);
    }

    private async Task<HashSet<string>> ResolveItemIdsWithExplicitHistoryAsync(
        string parkId,
        IReadOnlyCollection<ParkItem> items,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> closedItemIdsWithoutLifecycleHistory = items
            .Where(static item => !ParkItemsSitemapSectionProvider.IsPublicItem(item))
            .Where(static item => !AutomaticHistoryEventFactory.HasLifecycleDate(item))
            .Where(static item => !string.IsNullOrWhiteSpace(item.Id))
            .Select(static item => item.Id!)
            .ToList();
        if (closedItemIdsWithoutLifecycleHistory.Count == 0)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        IReadOnlyCollection<HistoryEvent> events = await this.historyEventRepository.GetParkTimelineAsync(
            parkId,
            includeHidden: false,
            includeParkItemEvents: true,
            closedItemIdsWithoutLifecycleHistory,
            cancellationToken);

        return events
            .Where(static historyEvent => historyEvent.IsVisible && historyEvent.EntityType == HistoryEntityType.ParkItem)
            .Select(static historyEvent => historyEvent.OwnerId)
            .Where(static ownerId => !string.IsNullOrWhiteSpace(ownerId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsPublicVideo(Video video, VideoOwnerType ownerType, string ownerId)
    {
        return !string.IsNullOrWhiteSpace(video.Id)
               && video.OwnerType == ownerType
               && string.Equals(video.OwnerId, ownerId, StringComparison.OrdinalIgnoreCase)
               && video.IsPublished;
    }

    private static bool HasPosition(Park park)
    {
        return park.Position is not null;
    }

    private static PublicHtmlSitemapNode CreateLeaf(string id, string label, string relativeUrl)
    {
        return new PublicHtmlSitemapNode
        {
            Id = id,
            Label = label,
            RelativeUrl = relativeUrl,
            HasChildren = false,
        };
    }

    private static string BuildParkPath(string language, Park park)
    {
        return $"/{language}/park/{park.Id}/{SeoSlugService.ToSlug(park.Name, "park")}";
    }

    private static string ResolveHistoryLabel(HistoryEvent historyEvent, string language)
    {
        string articleTitle = ResolveLocalizedText(historyEvent.Article?.Titles, language, string.Empty);
        if (!string.IsNullOrWhiteSpace(articleTitle))
        {
            return articleTitle;
        }

        return ResolveLocalizedText(historyEvent.Titles, language, historyEvent.Key);
    }

    private static string ResolveLocalizedText(
        IReadOnlyCollection<LocalizedText>? texts,
        string language,
        string fallback)
    {
        if (texts is not null)
        {
            LocalizedText? localizedText = texts.FirstOrDefault(text =>
                string.Equals(text.LanguageCode, language, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(text.Value));
            if (localizedText is not null)
            {
                return localizedText.Value!.Trim();
            }

            LocalizedText? defaultText = texts.FirstOrDefault(text =>
                string.Equals(text.LanguageCode, "en", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(text.Value));
            if (defaultText is not null)
            {
                return defaultText.Value!.Trim();
            }

            LocalizedText? firstText = texts.FirstOrDefault(static text => !string.IsNullOrWhiteSpace(text.Value));
            if (firstText is not null)
            {
                return firstText.Value!.Trim();
            }
        }

        return fallback;
    }

    private static string? NormalizeLanguage(string language, IReadOnlyCollection<string> supportedLanguages)
    {
        string normalizedLanguage = (language ?? string.Empty).Trim().ToLowerInvariant();
        IReadOnlyCollection<string> normalizedSupportedLanguages = supportedLanguages.Count == 0
            ? new[] { "en" }
            : supportedLanguages
                .Where(static supportedLanguage => !string.IsNullOrWhiteSpace(supportedLanguage))
                .Select(static supportedLanguage => supportedLanguage.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        return normalizedSupportedLanguages.Contains(normalizedLanguage, StringComparer.OrdinalIgnoreCase)
            ? normalizedLanguage
            : null;
    }

    private static string? TryReadNodeValue(string nodeId, string prefix)
    {
        string nodePrefix = $"{prefix}:";
        if (!nodeId.StartsWith(nodePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string value = nodeId[nodePrefix.Length..].Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
