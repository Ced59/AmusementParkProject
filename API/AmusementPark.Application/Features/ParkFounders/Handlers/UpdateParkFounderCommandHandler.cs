using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFounders.Commands;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFounders.Handlers;

/// <summary>
/// Handler de mise à jour d'un park founder.
/// </summary>
public sealed class UpdateParkFounderCommandHandler : ICommandHandler<UpdateParkFounderCommand, ApplicationResult<ParkFounder>>
{
    private readonly IParkFounderRepository repository;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="UpdateParkFounderCommandHandler"/>.
    /// </summary>
    public UpdateParkFounderCommandHandler(IParkFounderRepository repository, ISearchProjectionWriter searchProjectionWriter)
    {
        this.repository = repository;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<ParkFounder>> HandleAsync(UpdateParkFounderCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Id))
        {
            return ApplicationResult<ParkFounder>.Failure(ApplicationError.NotFound("park-founder.not-found", "Park founder not exists"));
        }

        if (command.ParkFounder is null)
        {
            return ApplicationResult<ParkFounder>.Failure(ApplicationErrors.Required(nameof(command.ParkFounder)));
        }

        try
        {
            ParkFounder? updated = await this.repository.UpdateAsync(command.Id, command.ParkFounder, cancellationToken);
            if (updated is null)
            {
                return ApplicationResult<ParkFounder>.Failure(ApplicationError.NotFound("park-founder.not-found", "Park founder not exists"));
            }

            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Founders, updated.Id, cancellationToken);
            return ApplicationResult<ParkFounder>.Success(updated);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult<ParkFounder>.Failure(ApplicationError.Technical("park-founder.update.failed", "Error while updating park founder"));
        }
    }
}
