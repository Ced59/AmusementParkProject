using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed class GetParkHistoryTimelineQueryHandler : IQueryHandler<GetParkHistoryTimelineQuery, ApplicationResult<HistoryTimelineResult>>
{
    private readonly IHistoryEventRepository historyEventRepository;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IImageRepository imageRepository;

    public GetParkHistoryTimelineQueryHandler(
        IHistoryEventRepository historyEventRepository,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IImageRepository imageRepository)
    {
        this.historyEventRepository = historyEventRepository;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.imageRepository = imageRepository;
    }

    public async Task<ApplicationResult<HistoryTimelineResult>> HandleAsync(GetParkHistoryTimelineQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<HistoryTimelineResult>.Failure(ApplicationErrors.Required("parkId"));
        }

        Park? park = await this.parkRepository.GetByIdAsync(query.ParkId.Trim(), query.IncludeHidden, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<HistoryTimelineResult>.Failure(ApplicationErrors.EntityNotFound(nameof(Park), query.ParkId));
        }

        if (!query.IncludeHidden && !HistoryPublicVisibility.IsPublicPark(park))
        {
            return ApplicationResult<HistoryTimelineResult>.Failure(ApplicationErrors.EntityNotFound(nameof(Park), query.ParkId));
        }

        IReadOnlyCollection<HistoryEvent> events = await this.historyEventRepository.GetParkTimelineAsync(
            park.Id,
            query.IncludeHidden,
            query.IncludeParkItemEvents,
            query.ParkItemIds,
            cancellationToken);
        IReadOnlyCollection<HistoryEvent> automaticParkEvents = AutomaticHistoryEventFactory.CreateParkLifecycleEvents(park);
        if (automaticParkEvents.Count > 0)
        {
            events = AutomaticHistoryEventFactory.MergeWithExplicitEvents(events, automaticParkEvents);
        }

        IReadOnlyCollection<ParkItem> automaticParkItemCandidates = query.IncludeParkItemEvents
            ? await this.LoadAutomaticParkItemCandidatesAsync(park.Id, query.IncludeHidden, query.ParkItemIds, cancellationToken)
            : Array.Empty<ParkItem>();

        if (automaticParkItemCandidates.Count > 0)
        {
            events = AutomaticHistoryEventFactory.MergeWithExplicitEvents(
                events,
                AutomaticHistoryEventFactory.CreateParkItemLifecycleEvents(automaticParkItemCandidates));
        }

        if (events.Count == 0)
        {
            return ApplicationResult<HistoryTimelineResult>.Failure(HistoryApplicationErrors.HistoryNotFound());
        }

        HistoryTimelineHydration hydration = await HistoryTimelineHydration.LoadAsync(
            events,
            this.parkRepository,
            this.parkItemRepository,
            this.imageRepository,
            cancellationToken);

        List<HistoryTimelineEventResult> timelineEvents = events
            .OrderBy(static item => item.Year)
            .ThenBy(static item => item.Month ?? 0)
            .ThenBy(static item => item.Day ?? 0)
            .ThenBy(static item => item.Key, StringComparer.Ordinal)
            .Select(hydration.ToTimelineEvent)
            .Where(entry => query.IncludeHidden || HistoryPublicVisibility.CanExposeTimelineEvent(entry, park))
            .ToList();

        if (timelineEvents.Count == 0)
        {
            return ApplicationResult<HistoryTimelineResult>.Failure(HistoryApplicationErrors.HistoryNotFound());
        }

        List<ParkItem> includedItems = timelineEvents
            .Select(static item => item.ParkItem)
            .Where(static item => item is not null)
            .Select(static item => item!)
            .DistinctBy(static item => item.Id)
            .OrderBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        bool hasParkItemTimelineEvents = includedItems.Count > 0;
        if (!hasParkItemTimelineEvents)
        {
            hasParkItemTimelineEvents = await this.historyEventRepository.HasParkItemTimelineEventsAsync(
                park.Id,
                query.IncludeHidden,
                cancellationToken);

            if (!hasParkItemTimelineEvents)
            {
                hasParkItemTimelineEvents = await this.HasAutomaticParkItemTimelineEventsAsync(
                    park.Id,
                    query.IncludeHidden,
                    cancellationToken);
            }
        }

        return ApplicationResult<HistoryTimelineResult>.Success(new HistoryTimelineResult
        {
            EntityType = HistoryEntityType.Park,
            Park = park,
            HasParkItemTimelineEvents = hasParkItemTimelineEvents,
            IncludedParkItems = includedItems,
            Events = timelineEvents,
        });
    }

    private async Task<IReadOnlyCollection<ParkItem>> LoadAutomaticParkItemCandidatesAsync(
        string parkId,
        bool includeHidden,
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkItem> parkItems = await this.parkItemRepository.GetByParkIdAsync(
            parkId,
            includeHidden,
            cancellationToken);
        HashSet<string> selectedParkItemIds = parkItemIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .ToHashSet(StringComparer.Ordinal);

        IEnumerable<ParkItem> candidates = parkItems;
        if (selectedParkItemIds.Count > 0)
        {
            candidates = candidates.Where(item => selectedParkItemIds.Contains(item.Id));
        }

        return candidates
            .Where(item => includeHidden || HistoryPublicVisibility.IsPublicParkItem(item))
            .Where(AutomaticHistoryEventFactory.HasLifecycleDate)
            .ToList();
    }

    private async Task<bool> HasAutomaticParkItemTimelineEventsAsync(
        string parkId,
        bool includeHidden,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkItem> parkItems = await this.parkItemRepository.GetByParkIdAsync(
            parkId,
            includeHidden,
            cancellationToken);
        return parkItems.Any(item =>
            (includeHidden || HistoryPublicVisibility.IsPublicParkItem(item)) &&
            AutomaticHistoryEventFactory.HasLifecycleDate(item));
    }
}

public sealed class GetParkItemHistoryTimelineQueryHandler : IQueryHandler<GetParkItemHistoryTimelineQuery, ApplicationResult<HistoryTimelineResult>>
{
    private readonly IHistoryEventRepository historyEventRepository;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IImageRepository imageRepository;

    public GetParkItemHistoryTimelineQueryHandler(
        IHistoryEventRepository historyEventRepository,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IImageRepository imageRepository)
    {
        this.historyEventRepository = historyEventRepository;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.imageRepository = imageRepository;
    }

    public async Task<ApplicationResult<HistoryTimelineResult>> HandleAsync(GetParkItemHistoryTimelineQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkItemId))
        {
            return ApplicationResult<HistoryTimelineResult>.Failure(ApplicationErrors.Required("parkItemId"));
        }

        ParkItem? parkItem = await this.parkItemRepository.GetByIdAsync(query.ParkItemId.Trim(), query.IncludeHidden, cancellationToken);
        if (parkItem is null)
        {
            return ApplicationResult<HistoryTimelineResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkItem), query.ParkItemId));
        }

        Park? park = string.IsNullOrWhiteSpace(parkItem.ParkId)
            ? null
            : await this.parkRepository.GetByIdAsync(parkItem.ParkId, query.IncludeHidden, cancellationToken);

        if (!query.IncludeHidden &&
            (!HistoryPublicVisibility.IsPublicParkItem(parkItem) || !HistoryPublicVisibility.IsPublicPark(park)))
        {
            return ApplicationResult<HistoryTimelineResult>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkItem), query.ParkItemId));
        }

        IReadOnlyCollection<HistoryEvent> events = await this.historyEventRepository.GetOwnerTimelineAsync(
            HistoryEntityType.ParkItem,
            parkItem.Id,
            query.IncludeHidden,
            cancellationToken);
        IReadOnlyCollection<HistoryEvent> automaticEvents = AutomaticHistoryEventFactory.CreateParkItemLifecycleEvents(parkItem);
        if (automaticEvents.Count > 0)
        {
            events = AutomaticHistoryEventFactory.MergeWithExplicitEvents(events, automaticEvents);
        }

        if (events.Count == 0)
        {
            return ApplicationResult<HistoryTimelineResult>.Failure(HistoryApplicationErrors.HistoryNotFound());
        }

        HistoryTimelineHydration hydration = await HistoryTimelineHydration.LoadAsync(
            events,
            this.parkRepository,
            this.parkItemRepository,
            this.imageRepository,
            cancellationToken);

        List<HistoryTimelineEventResult> timelineEvents = events
            .OrderBy(static item => item.Year)
            .ThenBy(static item => item.Month ?? 0)
            .ThenBy(static item => item.Day ?? 0)
            .ThenBy(static item => item.Key, StringComparer.Ordinal)
            .Select(hydration.ToTimelineEvent)
            .Where(entry => query.IncludeHidden || HistoryPublicVisibility.CanExposeTimelineEvent(entry, park))
            .ToList();

        if (timelineEvents.Count == 0)
        {
            return ApplicationResult<HistoryTimelineResult>.Failure(HistoryApplicationErrors.HistoryNotFound());
        }

        return ApplicationResult<HistoryTimelineResult>.Success(new HistoryTimelineResult
        {
            EntityType = HistoryEntityType.ParkItem,
            Park = park,
            ParkItem = parkItem,
            Events = timelineEvents,
        });
    }
}

public sealed class GetHistoryArticleQueryHandler : IQueryHandler<GetHistoryArticleQuery, ApplicationResult<HistoryArticleResult>>
{
    private readonly IHistoryEventRepository historyEventRepository;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IImageRepository imageRepository;

    public GetHistoryArticleQueryHandler(
        IHistoryEventRepository historyEventRepository,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IImageRepository imageRepository)
    {
        this.historyEventRepository = historyEventRepository;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.imageRepository = imageRepository;
    }

    public async Task<ApplicationResult<HistoryArticleResult>> HandleAsync(GetHistoryArticleQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.EventId))
        {
            return ApplicationResult<HistoryArticleResult>.Failure(ApplicationErrors.Required("eventId"));
        }

        HistoryEvent? historyEvent = await this.historyEventRepository.GetByIdAsync(query.EventId.Trim(), query.IncludeHidden, cancellationToken);
        if (historyEvent is null)
        {
            return ApplicationResult<HistoryArticleResult>.Failure(HistoryApplicationErrors.ArticleNotFound());
        }

        if (!historyEvent.IsMajor || historyEvent.Article is null || (!historyEvent.Article.IsPublished && !query.IncludeHidden))
        {
            return ApplicationResult<HistoryArticleResult>.Failure(HistoryApplicationErrors.ArticleNotFound());
        }

        HistoryTimelineHydration hydration = await HistoryTimelineHydration.LoadAsync(
            new[] { historyEvent },
            this.parkRepository,
            this.parkItemRepository,
            this.imageRepository,
            cancellationToken);

        HistoryTimelineEventResult hydratedEvent = hydration.ToTimelineEvent(historyEvent);
        Park? park = hydratedEvent.ContextPark;
        if (park is null && historyEvent.EntityType == HistoryEntityType.Park && !string.IsNullOrWhiteSpace(historyEvent.OwnerId))
        {
            park = await this.parkRepository.GetByIdAsync(historyEvent.OwnerId, query.IncludeHidden, cancellationToken);
        }

        if (park is null && hydratedEvent.ParkItem is not null && !string.IsNullOrWhiteSpace(hydratedEvent.ParkItem.ParkId))
        {
            park = await this.parkRepository.GetByIdAsync(hydratedEvent.ParkItem.ParkId, query.IncludeHidden, cancellationToken);
        }

        if (!query.IncludeHidden && !HistoryPublicVisibility.CanExposeTimelineEvent(hydratedEvent, park))
        {
            return ApplicationResult<HistoryArticleResult>.Failure(HistoryApplicationErrors.ArticleNotFound());
        }

        return ApplicationResult<HistoryArticleResult>.Success(new HistoryArticleResult
        {
            Event = historyEvent,
            Park = park,
            ParkItem = hydratedEvent.ParkItem,
            ContextPark = hydratedEvent.ContextPark,
            MainImage = hydratedEvent.MainImage,
        });
    }
}

public sealed class GetHistoryEventsPageQueryHandler : IQueryHandler<GetHistoryEventsPageQuery, ApplicationResult<PagedResult<HistoryTimelineEventResult>>>
{
    private readonly IHistoryEventRepository historyEventRepository;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IImageRepository imageRepository;

    public GetHistoryEventsPageQueryHandler(
        IHistoryEventRepository historyEventRepository,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IImageRepository imageRepository)
    {
        this.historyEventRepository = historyEventRepository;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.imageRepository = imageRepository;
    }

    public async Task<ApplicationResult<PagedResult<HistoryTimelineEventResult>>> HandleAsync(GetHistoryEventsPageQuery query, CancellationToken cancellationToken = default)
    {
        PagedResult<HistoryEvent> page = await this.historyEventRepository.GetAdminPageAsync(
            query.Paging.Page,
            query.Paging.PageSize,
            query.EntityType,
            query.OwnerId,
            query.Search,
            cancellationToken);

        HistoryTimelineHydration hydration = await HistoryTimelineHydration.LoadAsync(
            page.Items,
            this.parkRepository,
            this.parkItemRepository,
            this.imageRepository,
            cancellationToken);

        PagedResult<HistoryTimelineEventResult> result = new PagedResult<HistoryTimelineEventResult>(
            page.Items.Select(hydration.ToTimelineEvent).ToList(),
            page.Page,
            page.PageSize,
            page.TotalItems);

        return ApplicationResult<PagedResult<HistoryTimelineEventResult>>.Success(result);
    }
}

internal static class HistoryPublicVisibility
{
    public static bool CanExposeTimelineEvent(HistoryTimelineEventResult entry, Park? fallbackContextPark)
    {
        if (entry.Event.EntityType == HistoryEntityType.Park)
        {
            Park? park = entry.ContextPark ?? fallbackContextPark;
            return IsPublicPark(park);
        }

        if (entry.Event.EntityType == HistoryEntityType.ParkItem)
        {
            Park? contextPark = entry.ContextPark ?? fallbackContextPark;
            return IsPublicPark(contextPark) && IsPublicParkItem(entry.ParkItem);
        }

        return false;
    }

    public static bool IsPublicPark(Park? park)
    {
        return park is not null &&
               park.IsVisible &&
               park.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }

    public static bool IsPublicParkItem(ParkItem? parkItem)
    {
        return parkItem is not null &&
               parkItem.IsVisible &&
               parkItem.AdminReviewStatus != AdminReviewStatus.NotRelevant;
    }
}

internal sealed class HistoryTimelineHydration
{
    private readonly IReadOnlyDictionary<string, Park> parksById;
    private readonly IReadOnlyDictionary<string, ParkItem> parkItemsById;
    private readonly IReadOnlyDictionary<string, Image> imagesById;

    private HistoryTimelineHydration(
        IReadOnlyDictionary<string, Park> parksById,
        IReadOnlyDictionary<string, ParkItem> parkItemsById,
        IReadOnlyDictionary<string, Image> imagesById)
    {
        this.parksById = parksById;
        this.parkItemsById = parkItemsById;
        this.imagesById = imagesById;
    }

    public static async Task<HistoryTimelineHydration> LoadAsync(
        IReadOnlyCollection<HistoryEvent> events,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IImageRepository imageRepository,
        CancellationToken cancellationToken)
    {
        List<string> parkIds = events
            .SelectMany(static item => new[] { item.ParkId, item.ContextParkId }.Concat(item.RelatedParkIds))
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        List<string> parkItemIds = events
            .SelectMany(static item => new[] { item.ParkItemId }.Concat(item.RelatedParkItemIds))
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        List<string> imageIds = events
            .Select(static item => item.Article?.MainImageId ?? item.MainImageId)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        IReadOnlyCollection<Park> parks = parkIds.Count == 0
            ? Array.Empty<Park>()
            : await parkRepository.GetByIdsAsync(parkIds, cancellationToken);

        IReadOnlyCollection<ParkItem> parkItems = parkItemIds.Count == 0
            ? Array.Empty<ParkItem>()
            : await parkItemRepository.GetByIdsAsync(parkItemIds, cancellationToken);

        Dictionary<string, Image> imagesById = new Dictionary<string, Image>(StringComparer.Ordinal);
        foreach (string imageId in imageIds)
        {
            Image? image = await imageRepository.GetByIdAsync(imageId, cancellationToken);
            if (image is not null)
            {
                imagesById[image.Id] = image;
            }
        }

        return new HistoryTimelineHydration(
            parks
                .Where(static item => !string.IsNullOrWhiteSpace(item.Id))
                .ToDictionary(static item => item.Id, StringComparer.Ordinal),
            parkItems
                .Where(static item => !string.IsNullOrWhiteSpace(item.Id))
                .ToDictionary(static item => item.Id, StringComparer.Ordinal),
            imagesById);
    }

    public HistoryTimelineEventResult ToTimelineEvent(HistoryEvent historyEvent)
    {
        string? contextParkId = historyEvent.ContextParkId ?? historyEvent.ParkId;
        Park? contextPark = !string.IsNullOrWhiteSpace(contextParkId) && this.parksById.TryGetValue(contextParkId, out Park? resolvedPark)
            ? resolvedPark
            : null;

        string? parkItemId = historyEvent.ParkItemId;
        ParkItem? parkItem = !string.IsNullOrWhiteSpace(parkItemId) && this.parkItemsById.TryGetValue(parkItemId, out ParkItem? resolvedParkItem)
            ? resolvedParkItem
            : null;

        string? mainImageId = historyEvent.Article?.MainImageId ?? historyEvent.MainImageId;
        Image? mainImage = !string.IsNullOrWhiteSpace(mainImageId) && this.imagesById.TryGetValue(mainImageId, out Image? resolvedImage)
            ? resolvedImage
            : null;

        return new HistoryTimelineEventResult
        {
            Event = historyEvent,
            ContextPark = contextPark,
            ParkItem = parkItem,
            MainImage = mainImage,
        };
    }
}
