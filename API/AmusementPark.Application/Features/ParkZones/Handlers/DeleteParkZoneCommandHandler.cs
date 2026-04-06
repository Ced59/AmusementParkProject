using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkZones.Commands;
using AmusementPark.Application.Features.ParkZones.Ports;

namespace AmusementPark.Application.Features.ParkZones.Handlers;

public sealed class DeleteParkZoneCommandHandler : ICommandHandler<DeleteParkZoneCommand, ApplicationResult>
{
    private readonly IParkZoneRepository parkZoneRepository;

    public DeleteParkZoneCommandHandler(IParkZoneRepository parkZoneRepository)
    {
        this.parkZoneRepository = parkZoneRepository;
    }

    public async Task<ApplicationResult> HandleAsync(DeleteParkZoneCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ZoneId))
        {
            return ApplicationResult.Failure(ApplicationErrors.Required(nameof(command.ZoneId)));
        }

        bool deleted = await this.parkZoneRepository.DeleteAsync(command.ZoneId, cancellationToken);
        if (!deleted)
        {
            return ApplicationResult.Failure(ApplicationErrors.EntityNotFound("ParkZone", command.ZoneId));
        }

        return ApplicationResult.Success();
    }
}
