using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.ParkZones.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkZones.Handlers;

public sealed class GetParkZoneByIdQueryHandler : IQueryHandler<GetParkZoneByIdQuery, ApplicationResult<ParkZone>>
{
    private readonly IParkZoneRepository parkZoneRepository;

    public GetParkZoneByIdQueryHandler(IParkZoneRepository parkZoneRepository)
    {
        this.parkZoneRepository = parkZoneRepository;
    }

    public async Task<ApplicationResult<ParkZone>> HandleAsync(GetParkZoneByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ZoneId))
        {
            return ApplicationResult<ParkZone>.Failure(ApplicationErrors.Required(nameof(query.ZoneId)));
        }

        ParkZone? zone = await this.parkZoneRepository.GetByIdAsync(query.ZoneId, cancellationToken);
        if (zone is null)
        {
            return ApplicationResult<ParkZone>.Failure(ApplicationErrors.EntityNotFound("ParkZone", query.ZoneId));
        }

        return ApplicationResult<ParkZone>.Success(zone);
    }
}
