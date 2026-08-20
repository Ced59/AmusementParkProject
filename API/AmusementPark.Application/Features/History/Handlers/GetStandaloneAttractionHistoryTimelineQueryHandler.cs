using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed class GetStandaloneAttractionHistoryTimelineQueryHandler
    : IQueryHandler<GetStandaloneAttractionHistoryTimelineQuery, ApplicationResult<StandaloneAttractionHistoryTimelineResult>>
{
    private readonly IHistoryEventRepository historyEventRepository;
    private readonly IStandaloneAttractionRepository standaloneAttractionRepository;
    private readonly IImageRepository imageRepository;

    public GetStandaloneAttractionHistoryTimelineQueryHandler(
        IHistoryEventRepository historyEventRepository,
        IStandaloneAttractionRepository standaloneAttractionRepository,
        IImageRepository imageRepository)
    {
        this.historyEventRepository = historyEventRepository;
        this.standaloneAttractionRepository = standaloneAttractionRepository;
        this.imageRepository = imageRepository;
    }

    public async Task<ApplicationResult<StandaloneAttractionHistoryTimelineResult>> HandleAsync(
        GetStandaloneAttractionHistoryTimelineQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.StandaloneAttractionId))
        {
            return ApplicationResult<StandaloneAttractionHistoryTimelineResult>.Failure(ApplicationErrors.Required("standaloneAttractionId"));
        }

        string attractionId = query.StandaloneAttractionId.Trim();
        StandaloneAttraction? attraction = await this.standaloneAttractionRepository.GetByIdAsync(
            attractionId,
            query.IncludeHidden,
            cancellationToken);
        if (attraction is null || (!query.IncludeHidden && !attraction.IsPubliclyPublishable()))
        {
            return ApplicationResult<StandaloneAttractionHistoryTimelineResult>.Failure(
                ApplicationErrors.EntityNotFound(nameof(StandaloneAttraction), attractionId));
        }

        IReadOnlyCollection<HistoryEvent> events = await this.historyEventRepository.GetOwnerTimelineSummaryAsync(
            HistoryEntityType.StandaloneAttraction,
            attraction.Id,
            query.IncludeHidden,
            cancellationToken);
        IReadOnlyCollection<HistoryEvent> automaticEvents = AutomaticHistoryEventFactory.CreateStandaloneAttractionLifecycleEvents(attraction);
        if (automaticEvents.Count > 0)
        {
            events = AutomaticHistoryEventFactory.MergeWithExplicitEvents(events, automaticEvents);
        }

        if (!query.IncludeHidden)
        {
            events = events.Where(static historyEvent => historyEvent.IsVisible).ToList();
        }

        if (events.Count == 0)
        {
            return ApplicationResult<StandaloneAttractionHistoryTimelineResult>.Failure(HistoryApplicationErrors.HistoryNotFound());
        }

        List<HistoryTimelineEventResult> orderedEvents = events
            .OrderBy(static item => item.Year)
            .ThenBy(static item => item.Month ?? 0)
            .ThenBy(static item => item.Day ?? 0)
            .ThenBy(static item => item.Key, StringComparer.Ordinal)
            .Select(static historyEvent => new HistoryTimelineEventResult
            {
                Event = historyEvent,
            })
            .ToList();

        HistoryTimelinePageSlice? page = HistoryTimelinePageSlice.Create(orderedEvents, query.Page, query.PageSize);
        if (page is null)
        {
            return ApplicationResult<StandaloneAttractionHistoryTimelineResult>.Failure(HistoryApplicationErrors.HistoryNotFound());
        }

        List<HistoryTimelineEventResult> hydratedPageEvents = new List<HistoryTimelineEventResult>();
        foreach (HistoryTimelineEventResult timelineEvent in page.Events)
        {
            string? imageId = timelineEvent.Event.Article?.MainImageId ?? timelineEvent.Event.MainImageId;
            Image? mainImage = string.IsNullOrWhiteSpace(imageId)
                ? null
                : await this.imageRepository.GetByIdAsync(imageId, cancellationToken);

            hydratedPageEvents.Add(new HistoryTimelineEventResult
            {
                Event = timelineEvent.Event,
                MainImage = mainImage,
            });
        }

        return ApplicationResult<StandaloneAttractionHistoryTimelineResult>.Success(new StandaloneAttractionHistoryTimelineResult
        {
            StandaloneAttraction = attraction,
            Events = hydratedPageEvents,
            Pagination = page.Pagination,
            PageRanges = page.PageRanges,
        });
    }

}
