using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Contracts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkItems.Queries;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Application.Features.Parks;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class GetRelatedParkItemsQueryHandler
    : IQueryHandler<GetRelatedParkItemsQuery, ApplicationResult<IReadOnlyCollection<ParkItem>>>
{
    private const int DefaultLimit = 3;
    private const int MaximumLimit = 6;

    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkRepository parkRepository;

    public GetRelatedParkItemsQueryHandler(IParkItemRepository parkItemRepository, IParkRepository parkRepository)
    {
        this.parkItemRepository = parkItemRepository;
        this.parkRepository = parkRepository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<ParkItem>>> HandleAsync(GetRelatedParkItemsQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkItemId))
        {
            return ApplicationResult<IReadOnlyCollection<ParkItem>>.Failure(ApplicationErrors.Required(nameof(query.ParkItemId)));
        }

        ParkItem? currentItem = await this.parkItemRepository.GetByIdAsync(query.ParkItemId.Trim(), query.IncludeHidden, cancellationToken);
        if (currentItem is null)
        {
            return ApplicationResult<IReadOnlyCollection<ParkItem>>.Failure(ParkItemApplicationErrors.ParkItemNotExists());
        }

        if (!query.IncludeHidden)
        {
            Park? visiblePark = await this.parkRepository.GetByIdAsync(currentItem.ParkId, false, cancellationToken);
            if (visiblePark is null)
            {
                return ApplicationResult<IReadOnlyCollection<ParkItem>>.Failure(ParkApplicationErrors.ParkNotExists());
            }
        }

        int limit = query.Limit <= 0
            ? DefaultLimit
            : Math.Clamp(query.Limit, 1, MaximumLimit);

        IReadOnlyCollection<ParkItem> relatedItems = await this.parkItemRepository.GetRelatedItemsAsync(
            currentItem,
            limit,
            query.IncludeHidden,
            query.ClosedFilter,
            cancellationToken);

        return ApplicationResult<IReadOnlyCollection<ParkItem>>.Success(relatedItems);
    }
}
