using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkZones.Commands;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkZones.Handlers;

public sealed class UpdateParkZoneCommandHandler : ICommandHandler<UpdateParkZoneCommand, ApplicationResult<ParkZone>>
{
    private readonly IParkZoneRepository parkZoneRepository;

    public UpdateParkZoneCommandHandler(IParkZoneRepository parkZoneRepository)
    {
        this.parkZoneRepository = parkZoneRepository;
    }

    public async Task<ApplicationResult<ParkZone>> HandleAsync(UpdateParkZoneCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ZoneId))
        {
            return ApplicationResult<ParkZone>.Failure(ApplicationErrors.Required(nameof(command.ZoneId)));
        }

        if (command.Zone is null)
        {
            return ApplicationResult<ParkZone>.Failure(ApplicationErrors.Required(nameof(command.Zone)));
        }

        ParkZone? updated = await this.parkZoneRepository.UpdateAsync(command.ZoneId, command.Zone, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<ParkZone>.Failure(ApplicationErrors.EntityNotFound("ParkZone", command.ZoneId));
        }

        return ApplicationResult<ParkZone>.Success(updated);
    }
}
