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
