using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOperators.Commands;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;

namespace AmusementPark.Application.Features.ParkOperators.Handlers;

/// <summary>
/// Handler de mise à jour en masse du statut de revue admin des exploitants.
/// </summary>
public sealed class UpdateParkOperatorsBulkReviewStatusCommandHandler : ICommandHandler<UpdateParkOperatorsBulkReviewStatusCommand, ApplicationResult<BulkAdministrationUpdateResult>>
{
    private readonly IParkOperatorRepository repository;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    public UpdateParkOperatorsBulkReviewStatusCommandHandler(IParkOperatorRepository repository, ISearchProjectionWriter searchProjectionWriter)
    {
        this.repository = repository;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    public async Task<ApplicationResult<BulkAdministrationUpdateResult>> HandleAsync(UpdateParkOperatorsBulkReviewStatusCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Ids.Count == 0)
        {
            return ApplicationResult<BulkAdministrationUpdateResult>.Failure(ApplicationErrors.Required(nameof(command.Ids)));
        }

        int updatedCount = await this.repository.UpdateBulkAdminReviewStatusAsync(command.Ids, command.AdminReviewStatus, cancellationToken);
        foreach (string id in command.Ids.Where(static id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal))
        {
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Operators, id, cancellationToken);
        }

        return ApplicationResult<BulkAdministrationUpdateResult>.Success(new BulkAdministrationUpdateResult
        {
            RequestedCount = command.Ids.Count,
            UpdatedCount = updatedCount,
        });
    }
}
