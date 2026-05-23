using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.ParkZones.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkZones.Handlers;

public sealed class GetParkZoneByIdQueryHandler : IQueryHandler<GetParkZoneByIdQuery, ApplicationResult<ParkZone>>
{
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IParkRepository parkRepository;

    public GetParkZoneByIdQueryHandler(IParkZoneRepository parkZoneRepository, IParkRepository parkRepository)
    {
        this.parkZoneRepository = parkZoneRepository;
        this.parkRepository = parkRepository;
    }

    public async Task<ApplicationResult<ParkZone>> HandleAsync(GetParkZoneByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.ZoneId))
        {
            return ApplicationResult<ParkZone>.Failure(ParkZoneApplicationErrors.ParkZoneNotExists());
        }

        ParkZone? zone = await this.parkZoneRepository.GetByIdAsync(query.ZoneId.Trim(), cancellationToken);
        if (zone is null)
        {
            return ApplicationResult<ParkZone>.Failure(ParkZoneApplicationErrors.ParkZoneNotExists());
        }

        if (!query.IncludeHidden)
        {
            Park? visiblePark = await this.parkRepository.GetByIdAsync(zone.ParkId, false, cancellationToken);
            if (visiblePark is null)
            {
                return ApplicationResult<ParkZone>.Failure(ParkApplicationErrors.ParkNotExists());
            }
        }

        return ApplicationResult<ParkZone>.Success(zone);
    }
}
