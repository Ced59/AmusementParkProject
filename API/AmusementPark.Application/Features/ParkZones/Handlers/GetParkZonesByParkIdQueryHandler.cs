using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.ParkZones.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkZones.Handlers;

public sealed class GetParkZonesByParkIdQueryHandler : IQueryHandler<GetParkZonesByParkIdQuery, ApplicationResult<IReadOnlyCollection<ParkZone>>>
{
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IParkRepository parkRepository;

    public GetParkZonesByParkIdQueryHandler(IParkZoneRepository parkZoneRepository, IParkRepository parkRepository)
    {
        this.parkZoneRepository = parkZoneRepository;
        this.parkRepository = parkRepository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<ParkZone>>> HandleAsync(GetParkZonesByParkIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ParkId))
        {
            return ApplicationResult<IReadOnlyCollection<ParkZone>>.Failure(ParkApplicationErrors.ParkNotExists());
        }

        Park? park = await this.parkRepository.GetByIdAsync(query.ParkId.Trim(), query.IncludeHidden, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<IReadOnlyCollection<ParkZone>>.Failure(ParkApplicationErrors.ParkNotExists());
        }

        IReadOnlyCollection<ParkZone> zones = await this.parkZoneRepository.GetByParkIdAsync(query.ParkId.Trim(), cancellationToken);
        return ApplicationResult<IReadOnlyCollection<ParkZone>>.Success(zones);
    }
}
