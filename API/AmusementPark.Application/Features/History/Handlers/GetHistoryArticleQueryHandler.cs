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
