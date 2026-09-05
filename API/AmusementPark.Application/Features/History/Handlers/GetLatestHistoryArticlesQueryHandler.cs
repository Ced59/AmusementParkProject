using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed class GetLatestHistoryArticlesQueryHandler
    : IQueryHandler<GetLatestHistoryArticlesQuery, ApplicationResult<IReadOnlyCollection<HistoryArticleResult>>>
{
    private const int DefaultLimit = 3;
    private const int MinimumLimit = 1;
    private const int MaximumLimit = 3;
    private const int CandidatePageSize = 30;

    private readonly IHistoryEventRepository historyEventRepository;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IImageRepository imageRepository;

    public GetLatestHistoryArticlesQueryHandler(
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

    public async Task<ApplicationResult<IReadOnlyCollection<HistoryArticleResult>>> HandleAsync(
        GetLatestHistoryArticlesQuery query,
        CancellationToken cancellationToken = default)
    {
        int requestedLimit = query.Limit <= 0 ? DefaultLimit : query.Limit;
        int normalizedLimit = Math.Clamp(requestedLimit, MinimumLimit, MaximumLimit);
        List<HistoryArticleResult> articles = new List<HistoryArticleResult>(normalizedLimit);
        int offset = 0;

        while (articles.Count < normalizedLimit)
        {
            IReadOnlyCollection<HistoryEvent> candidateCollection = await this.historyEventRepository.GetLatestPublishedArticlesAsync(
                offset,
                CandidatePageSize,
                cancellationToken);
            List<HistoryEvent> candidates = candidateCollection.ToList();
            if (candidates.Count == 0)
            {
                break;
            }

            offset += candidates.Count;
            HistoryTimelineHydration hydration = await HistoryTimelineHydration.LoadAsync(
                candidates,
                this.parkRepository,
                this.parkItemRepository,
                this.imageRepository,
                includeImages: false,
                cancellationToken);
            List<HistoryTimelineEventResult> hydratedEvents = candidates
                .Select(hydration.ToTimelineEvent)
                .ToList();
            IReadOnlyDictionary<string, Park> fallbackParksById = await this.LoadFallbackParksByIdAsync(
                candidates,
                hydratedEvents,
                cancellationToken);

            for (int index = 0; index < candidates.Count; index++)
            {
                HistoryEvent historyEvent = candidates[index];
                HistoryTimelineEventResult hydratedEvent = hydratedEvents[index];
                Park? park = hydratedEvent.ContextPark ?? ResolveFallbackPark(
                    historyEvent,
                    hydratedEvent.ParkItem,
                    fallbackParksById);

                if (!HistoryPublicVisibility.CanExposeTimelineEvent(hydratedEvent, park))
                {
                    continue;
                }

                articles.Add(new HistoryArticleResult
                {
                    Event = historyEvent,
                    Park = historyEvent.EntityType == HistoryEntityType.Park ? park : null,
                    ParkItem = hydratedEvent.ParkItem,
                    ContextPark = park,
                });

                if (articles.Count == normalizedLimit)
                {
                    break;
                }
            }

            if (candidates.Count < CandidatePageSize)
            {
                break;
            }
        }

        return ApplicationResult<IReadOnlyCollection<HistoryArticleResult>>.Success(articles);
    }

    private async Task<IReadOnlyDictionary<string, Park>> LoadFallbackParksByIdAsync(
        IReadOnlyList<HistoryEvent> events,
        IReadOnlyList<HistoryTimelineEventResult> hydratedEvents,
        CancellationToken cancellationToken)
    {
        List<string> fallbackParkIds = new List<string>();

        for (int index = 0; index < events.Count; index++)
        {
            if (hydratedEvents[index].ContextPark is not null)
            {
                continue;
            }

            string? fallbackParkId = ResolveFallbackParkId(events[index], hydratedEvents[index].ParkItem);
            if (!string.IsNullOrWhiteSpace(fallbackParkId))
            {
                fallbackParkIds.Add(fallbackParkId.Trim());
            }
        }

        List<string> distinctParkIds = fallbackParkIds
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (distinctParkIds.Count == 0)
        {
            return new Dictionary<string, Park>(StringComparer.Ordinal);
        }

        IReadOnlyCollection<Park> parks = await this.parkRepository.GetByIdsAsync(distinctParkIds, cancellationToken);
        return parks
            .Where(static park => !string.IsNullOrWhiteSpace(park.Id))
            .ToDictionary(static park => park.Id, StringComparer.Ordinal);
    }

    private static Park? ResolveFallbackPark(
        HistoryEvent historyEvent,
        ParkItem? parkItem,
        IReadOnlyDictionary<string, Park> parksById)
    {
        string? parkId = ResolveFallbackParkId(historyEvent, parkItem);
        return !string.IsNullOrWhiteSpace(parkId) && parksById.TryGetValue(parkId.Trim(), out Park? park)
            ? park
            : null;
    }

    private static string? ResolveFallbackParkId(HistoryEvent historyEvent, ParkItem? parkItem)
    {
        return historyEvent.EntityType == HistoryEntityType.Park
            ? historyEvent.OwnerId
            : parkItem?.ParkId;
    }
}
