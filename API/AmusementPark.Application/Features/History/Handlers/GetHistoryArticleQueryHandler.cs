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
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.History.Handlers;

public sealed class GetHistoryArticleQueryHandler : IQueryHandler<GetHistoryArticleQuery, ApplicationResult<HistoryArticleResult>>
{
    private readonly IHistoryEventRepository historyEventRepository;
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IImageRepository imageRepository;
    private readonly IStandaloneAttractionRepository? standaloneAttractionRepository;

    public GetHistoryArticleQueryHandler(
        IHistoryEventRepository historyEventRepository,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IImageRepository imageRepository,
        IStandaloneAttractionRepository? standaloneAttractionRepository = null)
    {
        this.historyEventRepository = historyEventRepository;
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.imageRepository = imageRepository;
        this.standaloneAttractionRepository = standaloneAttractionRepository;
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

        StandaloneAttraction? standaloneAttraction = null;
        if (historyEvent.EntityType == HistoryEntityType.StandaloneAttraction)
        {
            standaloneAttraction = this.standaloneAttractionRepository is null
                ? null
                : await this.standaloneAttractionRepository.GetByIdAsync(historyEvent.OwnerId, query.IncludeHidden, cancellationToken);
            if (standaloneAttraction is null ||
                (!query.IncludeHidden && (!historyEvent.IsVisible || !standaloneAttraction.IsPubliclyPublishable())))
            {
                return ApplicationResult<HistoryArticleResult>.Failure(HistoryApplicationErrors.ArticleNotFound());
            }
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

        if (!query.IncludeHidden && standaloneAttraction is null && !HistoryPublicVisibility.CanExposeTimelineEvent(hydratedEvent, park))
        {
            return ApplicationResult<HistoryArticleResult>.Failure(HistoryApplicationErrors.ArticleNotFound());
        }

        return ApplicationResult<HistoryArticleResult>.Success(new HistoryArticleResult
        {
            Event = historyEvent,
            Park = park,
            ParkItem = hydratedEvent.ParkItem,
            ContextPark = hydratedEvent.ContextPark,
            StandaloneAttraction = standaloneAttraction,
            MainImage = hydratedEvent.MainImage,
        });
    }
}
