using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOperators.Commands;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOperators.Handlers;

/// <summary>
/// Handler de création d'un park operator.
/// </summary>
public sealed class CreateParkOperatorCommandHandler : ICommandHandler<CreateParkOperatorCommand, ApplicationResult<ParkOperator>>
{
    private readonly IParkOperatorRepository repository;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="CreateParkOperatorCommandHandler"/>.
    /// </summary>
    public CreateParkOperatorCommandHandler(IParkOperatorRepository repository)
    {
        this.repository = repository;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<ParkOperator>> HandleAsync(CreateParkOperatorCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ParkOperator is null)
        {
            return ApplicationResult<ParkOperator>.Failure(ApplicationErrors.Required(nameof(command.ParkOperator)));
        }

        ParkOperator created = await this.repository.CreateAsync(command.ParkOperator, cancellationToken);
        return ApplicationResult<ParkOperator>.Success(created);
    }
}
