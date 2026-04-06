using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Commands;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkZones.Handlers;

public sealed class CreateParkZoneCommandHandler : ICommandHandler<CreateParkZoneCommand, ApplicationResult<ParkZone>>
{
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IParkRepository parkRepository;

    public CreateParkZoneCommandHandler(IParkZoneRepository parkZoneRepository, IParkRepository parkRepository)
    {
        this.parkZoneRepository = parkZoneRepository;
        this.parkRepository = parkRepository;
    }

    public async Task<ApplicationResult<ParkZone>> HandleAsync(CreateParkZoneCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Zone is null)
        {
            return ApplicationResult<ParkZone>.Failure(ApplicationErrors.Required(nameof(command.Zone)));
        }

        if (string.IsNullOrWhiteSpace(command.Zone.ParkId))
        {
            return ApplicationResult<ParkZone>.Failure(ParkApplicationErrors.ParkNotExists());
        }

        Park? park = await this.parkRepository.GetByIdAsync(command.Zone.ParkId.Trim(), true, cancellationToken);
        if (park is null)
        {
            return ApplicationResult<ParkZone>.Failure(ParkApplicationErrors.ParkNotExists());
        }

        try
        {
            ParkZoneNaming.Normalize(command.Zone);
            ParkZone created = await this.parkZoneRepository.CreateAsync(command.Zone, cancellationToken);
            return ApplicationResult<ParkZone>.Success(created);
        }
        catch
        {
            return ApplicationResult<ParkZone>.Failure(ParkZoneApplicationErrors.ErrorCreatingParkZone());
        }
    }
}
