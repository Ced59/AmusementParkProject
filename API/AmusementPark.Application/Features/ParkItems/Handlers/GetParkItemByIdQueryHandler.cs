using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkItems.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class GetParkItemByIdQueryHandler : IQueryHandler<GetParkItemByIdQuery, ApplicationResult<ParkItem>>
{
    private readonly IParkItemRepository parkItemRepository;

    public GetParkItemByIdQueryHandler(IParkItemRepository parkItemRepository)
    {
        this.parkItemRepository = parkItemRepository;
    }

    public async Task<ApplicationResult<ParkItem>> HandleAsync(GetParkItemByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkItemId))
        {
            return ApplicationResult<ParkItem>.Failure(ApplicationErrors.Required(nameof(query.ParkItemId)));
        }

        ParkItem? item = await this.parkItemRepository.GetByIdAsync(query.ParkItemId, cancellationToken);
        if (item is null)
        {
            return ApplicationResult<ParkItem>.Failure(ApplicationErrors.EntityNotFound("ParkItem", query.ParkItemId));
        }

        return ApplicationResult<ParkItem>.Success(item);
    }
}
