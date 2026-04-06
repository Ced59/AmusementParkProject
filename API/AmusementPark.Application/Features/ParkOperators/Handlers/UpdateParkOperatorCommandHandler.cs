using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOperators.Commands;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOperators.Handlers;

/// <summary>
/// Handler de mise à jour d'un park operator.
/// </summary>
public sealed class UpdateParkOperatorCommandHandler : ICommandHandler<UpdateParkOperatorCommand, ApplicationResult<ParkOperator>>
{
    private readonly IParkOperatorRepository repository;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="UpdateParkOperatorCommandHandler"/>.
    /// </summary>
    public UpdateParkOperatorCommandHandler(IParkOperatorRepository repository)
    {
        this.repository = repository;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<ParkOperator>> HandleAsync(UpdateParkOperatorCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Id))
        {
            return ApplicationResult<ParkOperator>.Failure(ApplicationErrors.Required(nameof(command.Id)));
        }

        if (command.ParkOperator is null)
        {
            return ApplicationResult<ParkOperator>.Failure(ApplicationErrors.Required(nameof(command.ParkOperator)));
        }

        ParkOperator? updated = await this.repository.UpdateAsync(command.Id, command.ParkOperator, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<ParkOperator>.Failure(ApplicationErrors.EntityNotFound("ParkOperator", command.Id));
        }

        return ApplicationResult<ParkOperator>.Success(updated);
    }
}
