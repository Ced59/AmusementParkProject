using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOperators.Commands;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOperators.Handlers;

/// <summary>
/// Handler de mise à jour d'un park operator.
/// </summary>
public sealed class UpdateParkOperatorCommandHandler : ICommandHandler<UpdateParkOperatorCommand, ApplicationResult<ParkOperator>>
{
    private readonly IParkOperatorRepository repository;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="UpdateParkOperatorCommandHandler"/>.
    /// </summary>
    public UpdateParkOperatorCommandHandler(IParkOperatorRepository repository, ISearchProjectionWriter searchProjectionWriter)
    {
        this.repository = repository;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<ParkOperator>> HandleAsync(UpdateParkOperatorCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Id))
        {
            return ApplicationResult<ParkOperator>.Failure(ApplicationError.NotFound("park-operator.not-found", "Park operator not exists"));
        }

        if (command.ParkOperator is null)
        {
            return ApplicationResult<ParkOperator>.Failure(ApplicationErrors.Required(nameof(command.ParkOperator)));
        }

        try
        {
            ParkOperator? updated = await this.repository.UpdateAsync(command.Id, command.ParkOperator, cancellationToken);
            if (updated is null)
            {
                return ApplicationResult<ParkOperator>.Failure(ApplicationError.NotFound("park-operator.not-found", "Park operator not exists"));
            }

            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Operators, updated.Id, cancellationToken);
            return ApplicationResult<ParkOperator>.Success(updated);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult<ParkOperator>.Failure(ApplicationError.Technical("park-operator.update.failed", "Error while updating park operator"));
        }
    }
}
