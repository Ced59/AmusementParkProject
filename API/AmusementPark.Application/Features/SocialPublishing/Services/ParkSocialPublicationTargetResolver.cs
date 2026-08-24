using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.SocialPublishing.Contracts;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;

namespace AmusementPark.Application.Features.SocialPublishing.Services;

public sealed class ParkSocialPublicationTargetResolver
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IVideoRepository videoRepository;
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IHistoryEventRepository historyEventRepository;
    private readonly IQueryHandler<GetParkItemHistoryTimelineQuery, ApplicationResult<HistoryTimelineResult>> parkItemHistoryTimelineQueryHandler;

    public ParkSocialPublicationTargetResolver(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IVideoRepository videoRepository,
        IParkZoneRepository parkZoneRepository,
        IHistoryEventRepository historyEventRepository,
        IQueryHandler<GetParkItemHistoryTimelineQuery, ApplicationResult<HistoryTimelineResult>> parkItemHistoryTimelineQueryHandler)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.videoRepository = videoRepository;
        this.parkZoneRepository = parkZoneRepository;
        this.historyEventRepository = historyEventRepository;
        this.parkItemHistoryTimelineQueryHandler = parkItemHistoryTimelineQueryHandler;
    }

    internal async Task<ResolvedSocialPublicationTarget?> ResolveAsync(
        Uri normalizedUrl,
        IReadOnlyList<string> segments,
        CancellationToken cancellationToken)
    {
        if (segments.Count < 4 || string.IsNullOrWhiteSpace(segments[2]))
        {
            return null;
        }

        string parkId = segments[2];
        Park? park = await this.parkRepository.GetByIdAsync(parkId, false, cancellationToken);
        if (park is null || !park.IsPubliclyDiscoverable() || string.IsNullOrWhiteSpace(park.Name))
        {
            return null;
        }

        int itemSegmentIndex = segments.Count > 4
            && string.Equals(segments[4], "item", StringComparison.OrdinalIgnoreCase)
                ? 4
                : -1;
        int prospectiveEntityBaseLength = itemSegmentIndex < 0 ? 4 : itemSegmentIndex + 3;
        bool allowClosedItem = itemSegmentIndex >= 0
            && segments.Count > prospectiveEntityBaseLength
            && string.Equals(segments[prospectiveEntityBaseLength], "history", StringComparison.OrdinalIgnoreCase);
        ParkItem? item = await this.ResolveParkItemAsync(segments, itemSegmentIndex, parkId, allowClosedItem, cancellationToken);
        if (itemSegmentIndex >= 0 && item is null)
        {
            return null;
        }

        int entityBaseLength = item is null ? 4 : itemSegmentIndex + 3;
        ResolvedSocialPublicationTarget? videoTarget = await this.ResolveVideoTargetAsync(
            normalizedUrl,
            segments,
            entityBaseLength,
            parkId,
            item,
            cancellationToken);
        if (videoTarget is not null)
        {
            return videoTarget;
        }

        ImageOwnerType ownerType = item is null ? ImageOwnerType.Park : ImageOwnerType.ParkItem;
        string ownerId = item?.Id ?? parkId;
        ImageCategory category = item is null ? ImageCategory.Park : ImageCategory.ParkItem;
        if (segments.Count == entityBaseLength)
        {
            return new ResolvedSocialPublicationTarget(
                normalizedUrl,
                item is null ? SocialPublicationTargetKind.Park : SocialPublicationTargetKind.ParkItem,
                item?.Name ?? park.Name,
                item?.Name ?? park.Name,
                ownerType,
                ownerId,
                category,
                park);
        }

        SocialPublicationPageNames? names = await this.ResolvePageNamesAsync(
            segments,
            entityBaseLength,
            park,
            item,
            cancellationToken);
        if (names is null)
        {
            return null;
        }

        return new ResolvedSocialPublicationTarget(
            normalizedUrl,
            SocialPublicationTargetKind.Page,
            names.French,
            names.English,
            ownerType,
            ownerId,
            category,
            park);
    }

    private async Task<ParkItem?> ResolveParkItemAsync(
        IReadOnlyList<string> segments,
        int itemSegmentIndex,
        string parkId,
        bool allowClosedItem,
        CancellationToken cancellationToken)
    {
        if (itemSegmentIndex < 0)
        {
            return null;
        }

        if (itemSegmentIndex + 2 >= segments.Count)
        {
            return null;
        }

        ParkItem? item = await this.parkItemRepository.GetByIdAsync(
            segments[itemSegmentIndex + 1],
            false,
            cancellationToken);
        return item is not null
            && !string.IsNullOrWhiteSpace(item.Id)
            && item.IsVisible
            && item.AdminReviewStatus != AdminReviewStatus.NotRelevant
            && (allowClosedItem || !ParkItemStatusNormalizer.IsClosedDefinitively(item.AttractionDetails?.Status))
            && string.Equals(item.ParkId, parkId, StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(item.Name)
                ? item
                : null;
    }

    private async Task<ResolvedSocialPublicationTarget?> ResolveVideoTargetAsync(
        Uri normalizedUrl,
        IReadOnlyList<string> segments,
        int entityBaseLength,
        string parkId,
        ParkItem? item,
        CancellationToken cancellationToken)
    {
        bool isVideoDetailRoute = segments.Count == entityBaseLength + 3
            && string.Equals(segments[entityBaseLength], "videos", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(segments[entityBaseLength + 1])
            && !string.IsNullOrWhiteSpace(segments[entityBaseLength + 2]);
        if (!isVideoDetailRoute)
        {
            return null;
        }

        Video? video = await this.videoRepository.GetByIdAsync(segments[entityBaseLength + 1], cancellationToken);
        if (video is null || !video.IsPublished || string.IsNullOrWhiteSpace(video.Title))
        {
            return null;
        }

        if (item is not null
            && (video.OwnerType != VideoOwnerType.ParkItem
                || !string.Equals(video.OwnerId, item.Id, StringComparison.Ordinal)))
        {
            return null;
        }

        if (item is null
            && (video.OwnerType != VideoOwnerType.Park
                || !string.Equals(video.OwnerId, parkId, StringComparison.Ordinal)))
        {
            return null;
        }

        return new ResolvedSocialPublicationTarget(
            normalizedUrl,
            SocialPublicationTargetKind.Video,
            SocialPublicationLocalizedTextResolver.Resolve(video.Titles, "fr", video.Title),
            SocialPublicationLocalizedTextResolver.Resolve(video.Titles, "en", video.Title),
            item is null ? ImageOwnerType.Park : ImageOwnerType.ParkItem,
            item?.Id ?? parkId,
            item is null ? ImageCategory.Park : ImageCategory.ParkItem,
            null);
    }

    private async Task<SocialPublicationPageNames?> ResolvePageNamesAsync(
        IReadOnlyList<string> segments,
        int entityBaseLength,
        Park park,
        ParkItem? item,
        CancellationToken cancellationToken)
    {
        int suffixLength = segments.Count - entityBaseLength;
        if (suffixLength == 3
            && string.Equals(segments[entityBaseLength], "history", StringComparison.OrdinalIgnoreCase)
            && string.Equals(segments[entityBaseLength + 1], "page", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(segments[entityBaseLength + 2], out int page)
            && page > 0)
        {
            if (item is not null && !await this.HasPublicParkItemHistoryPageAsync(item.Id!, page, cancellationToken))
            {
                return null;
            }

            string historyEntityName = item?.Name ?? park.Name ?? string.Empty;
            return new SocialPublicationPageNames($"L’histoire de {historyEntityName}", $"The history of {historyEntityName}");
        }

        if (suffixLength == 3
            && item is null
            && string.Equals(segments[entityBaseLength], "zone", StringComparison.OrdinalIgnoreCase))
        {
            return await this.ResolveZoneNamesAsync(segments[entityBaseLength + 1], park.Id!, cancellationToken);
        }

        if (suffixLength == 3
            && string.Equals(segments[entityBaseLength], "history", StringComparison.OrdinalIgnoreCase))
        {
            return await this.ResolveHistoryArticleNamesAsync(
                segments[entityBaseLength + 1],
                park.Id!,
                item,
                cancellationToken);
        }

        if (suffixLength != 1)
        {
            return null;
        }

        string entityName = item?.Name ?? park.Name ?? string.Empty;
        string section = segments[entityBaseLength].ToLowerInvariant();
        if (item is not null
            && string.Equals(section, "history", StringComparison.Ordinal)
            && !await this.HasPublicParkItemHistoryPageAsync(item.Id!, 1, cancellationToken))
        {
            return null;
        }

        return section switch
        {
            "images" => new SocialPublicationPageNames($"Les photos de {entityName}", $"{entityName} photos"),
            "history" => new SocialPublicationPageNames($"L’histoire de {entityName}", $"The history of {entityName}"),
            "videos" => new SocialPublicationPageNames($"Les vidéos de {entityName}", $"{entityName} videos"),
            "comments" => new SocialPublicationPageNames($"Les avis sur {entityName}", $"Reviews of {entityName}"),
            "map" when item is null => new SocialPublicationPageNames($"La carte de {entityName}", $"The map of {entityName}"),
            "zones" when item is null => new SocialPublicationPageNames($"Les zones de {entityName}", $"Areas at {entityName}"),
            "weather" when item is null => new SocialPublicationPageNames($"La météo de {entityName}", $"The weather at {entityName}"),
            "opening-hours" when item is null => new SocialPublicationPageNames($"Les horaires de {entityName}", $"Opening hours for {entityName}"),
            "pricing" when item is null => new SocialPublicationPageNames($"Les tarifs de {entityName}", $"Admission prices for {entityName}"),
            "items" when item is null => new SocialPublicationPageNames($"Les attractions et lieux de {entityName}", $"Attractions and places at {entityName}"),
            _ => null,
        };
    }

    private async Task<bool> HasPublicParkItemHistoryPageAsync(
        string parkItemId,
        int page,
        CancellationToken cancellationToken)
    {
        ApplicationResult<HistoryTimelineResult> result = await this.parkItemHistoryTimelineQueryHandler.HandleAsync(
            new GetParkItemHistoryTimelineQuery(parkItemId, IncludeHidden: false, Page: page),
            cancellationToken);
        return result.IsSuccess && result.Value is not null;
    }

    private async Task<SocialPublicationPageNames?> ResolveZoneNamesAsync(
        string zoneId,
        string parkId,
        CancellationToken cancellationToken)
    {
        ParkZone? zone = await this.parkZoneRepository.GetByIdAsync(zoneId, cancellationToken);
        if (zone is null
            || !zone.IsVisible
            || string.IsNullOrWhiteSpace(zone.Id)
            || string.IsNullOrWhiteSpace(zone.Name)
            || !string.Equals(zone.ParkId, parkId, StringComparison.Ordinal))
        {
            return null;
        }

        return new SocialPublicationPageNames(
            SocialPublicationLocalizedTextResolver.Resolve(zone.Names, "fr", zone.Name),
            SocialPublicationLocalizedTextResolver.Resolve(zone.Names, "en", zone.Name));
    }

    private async Task<SocialPublicationPageNames?> ResolveHistoryArticleNamesAsync(
        string eventId,
        string parkId,
        ParkItem? item,
        CancellationToken cancellationToken)
    {
        HistoryEvent? historyEvent = await this.historyEventRepository.GetByIdAsync(
            eventId,
            false,
            cancellationToken);
        if (historyEvent is null
            || !historyEvent.IsVisible
            || !historyEvent.IsMajor
            || historyEvent.Article is null
            || !historyEvent.Article.IsPublished)
        {
            return null;
        }

        bool ownsArticle = item is null
            ? historyEvent.EntityType == HistoryEntityType.Park
                && string.Equals(historyEvent.OwnerId, parkId, StringComparison.Ordinal)
            : historyEvent.EntityType == HistoryEntityType.ParkItem
                && string.Equals(historyEvent.OwnerId, item.Id, StringComparison.Ordinal);
        if (!ownsArticle)
        {
            return null;
        }

        string fallback = item?.Name ?? "Article historique";
        return new SocialPublicationPageNames(
            SocialPublicationLocalizedTextResolver.Resolve(
                historyEvent.Article.Titles.Count > 0 ? historyEvent.Article.Titles : historyEvent.Titles,
                "fr",
                fallback),
            SocialPublicationLocalizedTextResolver.Resolve(
                historyEvent.Article.Titles.Count > 0 ? historyEvent.Article.Titles : historyEvent.Titles,
                "en",
                fallback));
    }
}
