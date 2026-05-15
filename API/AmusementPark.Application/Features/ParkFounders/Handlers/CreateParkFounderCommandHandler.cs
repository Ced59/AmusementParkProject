using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFounders.Commands;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFounders.Handlers;

/// <summary>
/// Handler de création d'un park founder.
/// </summary>
public sealed class CreateParkFounderCommandHandler : ICommandHandler<CreateParkFounderCommand, ApplicationResult<ParkFounder>>
{
    private readonly IParkFounderRepository repository;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    /// <summary>
    /// Initialise une nouvelle instance de la classe <see cref="CreateParkFounderCommandHandler"/>.
    /// </summary>
    public CreateParkFounderCommandHandler(IParkFounderRepository repository, ISearchProjectionWriter searchProjectionWriter)
    {
        this.repository = repository;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    /// <inheritdoc />
    public async Task<ApplicationResult<ParkFounder>> HandleAsync(CreateParkFounderCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ParkFounder is null)
        {
            return ApplicationResult<ParkFounder>.Failure(ApplicationErrors.Required(nameof(command.ParkFounder)));
        }

        try
        {
            ParkFounder created = await this.repository.CreateAsync(command.ParkFounder, cancellationToken);
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Founders, created.Id, cancellationToken);
            return ApplicationResult<ParkFounder>.Success(created);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult<ParkFounder>.Failure(ApplicationError.Technical("park-founder.create.failed", "Error while creating park founder"));
        }
    }
}
