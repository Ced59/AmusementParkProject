using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Commands;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;

namespace AmusementPark.Application.Features.AttractionManufacturers.Handlers;

/// <summary>
/// Handler de mise à jour en masse du statut de revue admin des constructeurs.
/// </summary>
public sealed class UpdateAttractionManufacturersBulkReviewStatusCommandHandler : ICommandHandler<UpdateAttractionManufacturersBulkReviewStatusCommand, ApplicationResult<BulkAdministrationUpdateResult>>
{
    private readonly IAttractionManufacturerRepository repository;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    public UpdateAttractionManufacturersBulkReviewStatusCommandHandler(IAttractionManufacturerRepository repository, ISearchProjectionWriter searchProjectionWriter)
    {
        this.repository = repository;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    public async Task<ApplicationResult<BulkAdministrationUpdateResult>> HandleAsync(UpdateAttractionManufacturersBulkReviewStatusCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Ids.Count == 0)
        {
            return ApplicationResult<BulkAdministrationUpdateResult>.Failure(ApplicationErrors.Required(nameof(command.Ids)));
        }

        int updatedCount = await this.repository.UpdateBulkAdminReviewStatusAsync(command.Ids, command.AdminReviewStatus, cancellationToken);
        foreach (string id in command.Ids.Where(static id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal))
        {
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, id, cancellationToken);
        }

        return ApplicationResult<BulkAdministrationUpdateResult>.Success(new BulkAdministrationUpdateResult
        {
            RequestedCount = command.Ids.Count,
            UpdatedCount = updatedCount,
        });
    }
}
