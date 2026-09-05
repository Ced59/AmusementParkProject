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
    private const int CandidateMultiplier = 10;

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
        IReadOnlyCollection<HistoryEvent> candidates = await this.historyEventRepository.GetLatestPublishedArticlesAsync(
            normalizedLimit * CandidateMultiplier,
            cancellationToken);

        HistoryTimelineHydration hydration = await HistoryTimelineHydration.LoadAsync(
            candidates,
            this.parkRepository,
            this.parkItemRepository,
            this.imageRepository,
            includeImages: false,
            cancellationToken);
        List<HistoryArticleResult> articles = new List<HistoryArticleResult>(normalizedLimit);

        foreach (HistoryEvent historyEvent in candidates)
        {
            HistoryTimelineEventResult hydratedEvent = hydration.ToTimelineEvent(historyEvent);
            Park? park = hydratedEvent.ContextPark;

            if (!HistoryPublicVisibility.CanExposeTimelineEvent(hydratedEvent, park))
            {
                continue;
            }

            articles.Add(new HistoryArticleResult
            {
                Event = historyEvent,
                Park = historyEvent.EntityType == HistoryEntityType.Park ? park : null,
                ParkItem = hydratedEvent.ParkItem,
                ContextPark = hydratedEvent.ContextPark,
            });

            if (articles.Count == normalizedLimit)
            {
                break;
            }
        }

        return ApplicationResult<IReadOnlyCollection<HistoryArticleResult>>.Success(articles);
    }
}
