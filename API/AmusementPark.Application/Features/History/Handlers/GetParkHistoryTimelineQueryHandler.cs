using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.History;
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

        IReadOnlyCollection<HistoryEvent> events = await this.historyEventRepository.GetParkTimelineSummaryAsync(
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
            false,
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

        HistoryTimelinePageSlice? page = HistoryTimelinePageSlice.Create(timelineEvents, query.Page, query.PageSize);
        if (page is null)
        {
            return ApplicationResult<HistoryTimelineResult>.Failure(HistoryApplicationErrors.HistoryNotFound());
        }

        HistoryTimelineHydration pageHydration = await HistoryTimelineHydration.LoadAsync(
            page.Events.Select(static item => item.Event).ToList(),
            this.parkRepository,
            this.parkItemRepository,
            this.imageRepository,
            cancellationToken);
        List<HistoryTimelineEventResult> pageEvents = page.Events
            .Select(item => pageHydration.ToTimelineEvent(item.Event))
            .ToList();

        List<ParkItem> includedItems = pageEvents
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
            Events = pageEvents,
            Pagination = page.Pagination,
            PageRanges = page.PageRanges,
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
