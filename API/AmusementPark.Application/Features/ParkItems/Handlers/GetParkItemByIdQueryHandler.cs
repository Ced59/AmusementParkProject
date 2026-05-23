using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkItems.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class GetParkItemByIdQueryHandler : IQueryHandler<GetParkItemByIdQuery, ApplicationResult<ParkItem>>
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkRepository parkRepository;

    public GetParkItemByIdQueryHandler(IParkItemRepository parkItemRepository, IParkRepository parkRepository)
    {
        this.parkItemRepository = parkItemRepository;
        this.parkRepository = parkRepository;
    }

    public async Task<ApplicationResult<ParkItem>> HandleAsync(GetParkItemByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkItemId))
        {
            return ApplicationResult<ParkItem>.Failure(ApplicationErrors.Required(nameof(query.ParkItemId)));
        }

        ParkItem? item = await this.parkItemRepository.GetByIdAsync(query.ParkItemId, query.IncludeHidden, cancellationToken);
        if (item is null)
        {
            return ApplicationResult<ParkItem>.Failure(ParkItemApplicationErrors.ParkItemNotExists());
        }

        if (!query.IncludeHidden)
        {
            Park? visiblePark = await this.parkRepository.GetByIdAsync(item.ParkId, false, cancellationToken);
            if (visiblePark is null)
            {
                return ApplicationResult<ParkItem>.Failure(ParkApplicationErrors.ParkNotExists());
            }
        }

        return ApplicationResult<ParkItem>.Success(item);
    }
}
