using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Commands;
using AmusementPark.Application.Features.StandaloneAttractions.Contracts;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Queries;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.StandaloneAttractions.Handlers;

public sealed class UpdateStandaloneAttractionCommandHandler
    : ICommandHandler<UpdateStandaloneAttractionCommand, ApplicationResult<StandaloneAttraction>>
{
    private readonly IStandaloneAttractionRepository repository;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    public UpdateStandaloneAttractionCommandHandler(IStandaloneAttractionRepository repository, ISearchProjectionWriter searchProjectionWriter)
    {
        this.repository = repository;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    public async Task<ApplicationResult<StandaloneAttraction>> HandleAsync(UpdateStandaloneAttractionCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Id))
        {
            return ApplicationResult<StandaloneAttraction>.Failure(ApplicationErrors.Required(nameof(command.Id)));
        }

        if (command.Attraction is null)
        {
            return ApplicationResult<StandaloneAttraction>.Failure(ApplicationErrors.Required(nameof(command.Attraction)));
        }

        CreateStandaloneAttractionCommandHandler.Normalize(command.Attraction);
        StandaloneAttraction? updated = await this.repository.UpdateAsync(command.Id.Trim(), command.Attraction, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<StandaloneAttraction>.Failure(ApplicationErrors.EntityNotFound(nameof(StandaloneAttraction), command.Id));
        }

        if (!string.IsNullOrWhiteSpace(updated.Id))
        {
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.StandaloneAttractions, updated.Id, cancellationToken);
        }

        return ApplicationResult<StandaloneAttraction>.Success(updated);
    }
}
