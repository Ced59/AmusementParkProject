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

        IReadOnlyCollection<HistoryEvent> events = await this.historyEventRepository.GetOwnerTimelineSummaryAsync(
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

        return ApplicationResult<HistoryTimelineResult>.Success(new HistoryTimelineResult
        {
            EntityType = HistoryEntityType.ParkItem,
            Park = park,
            ParkItem = parkItem,
            Events = pageEvents,
            Pagination = page.Pagination,
            PageRanges = page.PageRanges,
        });
    }
}
