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

public sealed class UpdateStandaloneAttractionsBulkAdministrationCommandHandler
    : ICommandHandler<UpdateStandaloneAttractionsBulkAdministrationCommand, ApplicationResult<BulkAdministrationUpdateResult>>
{
    private readonly IStandaloneAttractionRepository repository;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    public UpdateStandaloneAttractionsBulkAdministrationCommandHandler(IStandaloneAttractionRepository repository, ISearchProjectionWriter searchProjectionWriter)
    {
        this.repository = repository;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    public async Task<ApplicationResult<BulkAdministrationUpdateResult>> HandleAsync(UpdateStandaloneAttractionsBulkAdministrationCommand command, CancellationToken cancellationToken = default)
    {
        List<string> ids = command.Ids
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (ids.Count == 0)
        {
            return ApplicationResult<BulkAdministrationUpdateResult>.Failure(ApplicationErrors.Required(nameof(command.Ids)));
        }

        if (!command.IsVisible.HasValue && !command.AdminReviewStatus.HasValue)
        {
            return ApplicationResult<BulkAdministrationUpdateResult>.Failure(ApplicationErrors.Required("bulkAction"));
        }

        int updatedCount = await this.repository.UpdateBulkAdministrationAsync(ids, command.IsVisible, command.AdminReviewStatus, cancellationToken);
        if (updatedCount > 0)
        {
            await this.searchProjectionWriter.UpsertManyAsync(SearchProjectionResourceTypes.StandaloneAttractions, ids, cancellationToken);
        }

        return ApplicationResult<BulkAdministrationUpdateResult>.Success(new BulkAdministrationUpdateResult
        {
            RequestedCount = ids.Count,
            UpdatedCount = updatedCount,
        });
    }
}
