using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Commands;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkZones.Handlers;

public sealed class UpdateParkZoneCommandHandler : ICommandHandler<UpdateParkZoneCommand, ApplicationResult<ParkZone>>
{
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IParkRepository parkRepository;

    public UpdateParkZoneCommandHandler(IParkZoneRepository parkZoneRepository, IParkRepository parkRepository)
    {
        this.parkZoneRepository = parkZoneRepository;
        this.parkRepository = parkRepository;
    }

    public async Task<ApplicationResult<ParkZone>> HandleAsync(UpdateParkZoneCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ZoneId))
        {
            return ApplicationResult<ParkZone>.Failure(ParkZoneApplicationErrors.ParkZoneNotExists());
        }

        if (command.Zone is null)
        {
            return ApplicationResult<ParkZone>.Failure(ApplicationErrors.Required(nameof(command.Zone)));
        }

        ParkZone? existing = await this.parkZoneRepository.GetByIdAsync(command.ZoneId.Trim(), cancellationToken);
        if (existing is null)
        {
            return ApplicationResult<ParkZone>.Failure(ParkZoneApplicationErrors.ParkZoneNotExists());
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
            command.Zone.Id = existing.Id;
            command.Zone.CreatedAtUtc = existing.CreatedAtUtc;
            ParkZoneNaming.Normalize(command.Zone, existing.Name);
            ParkZone? updated = await this.parkZoneRepository.UpdateAsync(command.ZoneId.Trim(), command.Zone, cancellationToken);
            if (updated is null)
            {
                return ApplicationResult<ParkZone>.Failure(ParkZoneApplicationErrors.ErrorUpdatingParkZone());
            }

            return ApplicationResult<ParkZone>.Success(updated);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult<ParkZone>.Failure(ParkZoneApplicationErrors.ErrorUpdatingParkZone());
        }
    }
}
