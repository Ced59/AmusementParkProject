using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Commands;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de mise à jour d'un parc.
/// </summary>
public sealed class UpdateParkCommandHandler : ICommandHandler<UpdateParkCommand, ApplicationResult<Park>>
{
    private readonly IParkRepository parkRepository;

    public UpdateParkCommandHandler(IParkRepository parkRepository)
    {
        this.parkRepository = parkRepository;
    }

    public async Task<ApplicationResult<Park>> HandleAsync(UpdateParkCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ParkId))
        {
            return ApplicationResult<Park>.Failure(ApplicationErrors.Required(nameof(command.ParkId)));
        }

        if (command.Park is null)
        {
            return ApplicationResult<Park>.Failure(ApplicationErrors.Required(nameof(command.Park)));
        }

        Park? updated = await this.parkRepository.UpdateAsync(command.ParkId, command.Park, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<Park>.Failure(ApplicationErrors.EntityNotFound("Park", command.ParkId));
        }

        return ApplicationResult<Park>.Success(updated);
    }
}
