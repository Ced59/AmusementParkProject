using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkZones.Commands;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkZones.Handlers;

public sealed class CreateParkZoneCommandHandler : ICommandHandler<CreateParkZoneCommand, ApplicationResult<ParkZone>>
{
    private readonly IParkZoneRepository parkZoneRepository;

    public CreateParkZoneCommandHandler(IParkZoneRepository parkZoneRepository)
    {
        this.parkZoneRepository = parkZoneRepository;
    }

    public async Task<ApplicationResult<ParkZone>> HandleAsync(CreateParkZoneCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Zone is null)
        {
            return ApplicationResult<ParkZone>.Failure(ApplicationErrors.Required(nameof(command.Zone)));
        }

        ParkZone created = await this.parkZoneRepository.CreateAsync(command.Zone, cancellationToken);
        return ApplicationResult<ParkZone>.Success(created);
    }
}
