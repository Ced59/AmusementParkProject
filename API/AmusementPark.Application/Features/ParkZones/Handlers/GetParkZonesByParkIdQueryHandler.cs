using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.ParkZones.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkZones.Handlers;

public sealed class GetParkZonesByParkIdQueryHandler : IQueryHandler<GetParkZonesByParkIdQuery, ApplicationResult<IReadOnlyCollection<ParkZone>>>
{
    private readonly IParkZoneRepository parkZoneRepository;

    public GetParkZonesByParkIdQueryHandler(IParkZoneRepository parkZoneRepository)
    {
        this.parkZoneRepository = parkZoneRepository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<ParkZone>>> HandleAsync(GetParkZonesByParkIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<IReadOnlyCollection<ParkZone>>.Failure(ApplicationErrors.Required(nameof(query.ParkId)));
        }

        IReadOnlyCollection<ParkZone> zones = await this.parkZoneRepository.GetByParkIdAsync(query.ParkId, cancellationToken);
        return ApplicationResult<IReadOnlyCollection<ParkZone>>.Success(zones);
    }
}
