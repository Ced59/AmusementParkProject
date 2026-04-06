using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Commands;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de mise à jour de visibilité d'un parc.
/// </summary>
public sealed class UpdateParkVisibilityCommandHandler : ICommandHandler<UpdateParkVisibilityCommand, ApplicationResult<Park>>
{
    private readonly IParkRepository parkRepository;

    public UpdateParkVisibilityCommandHandler(IParkRepository parkRepository)
    {
        this.parkRepository = parkRepository;
    }

    public async Task<ApplicationResult<Park>> HandleAsync(UpdateParkVisibilityCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ParkId))
        {
            return ApplicationResult<Park>.Failure(ApplicationErrors.Required(nameof(command.ParkId)));
        }

        Park? updated = await this.parkRepository.UpdateVisibilityAsync(command.ParkId, command.IsVisible, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<Park>.Failure(ApplicationErrors.EntityNotFound("Park", command.ParkId));
        }

        return ApplicationResult<Park>.Success(updated);
    }
}
