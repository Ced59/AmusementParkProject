using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkZones.Commands;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.Parks;

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
            return ApplicationResult.Failure(ParkZoneApplicationErrors.ParkZoneNotExists());
        }

        ParkZone? existing = await this.parkZoneRepository.GetByIdAsync(command.ZoneId.Trim(), cancellationToken);
        if (existing is null)
        {
            return ApplicationResult.Failure(ParkZoneApplicationErrors.ParkZoneNotExists());
        }

        try
        {
            bool deleted = await this.parkZoneRepository.DeleteAsync(command.ZoneId.Trim(), cancellationToken);
            if (!deleted)
            {
                return ApplicationResult.Failure(ParkZoneApplicationErrors.ErrorDeletingParkZone());
            }

            return ApplicationResult.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult.Failure(ParkZoneApplicationErrors.ErrorDeletingParkZone());
        }
    }
}
