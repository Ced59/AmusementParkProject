using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

/// <summary>
/// Handler d'action de masse d'administration sur les park items.
/// </summary>
public sealed class UpdateParkItemsBulkAdministrationCommandHandler : ICommandHandler<UpdateParkItemsBulkAdministrationCommand, ApplicationResult<BulkAdministrationUpdateResult>>
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    public UpdateParkItemsBulkAdministrationCommandHandler(IParkItemRepository parkItemRepository, ISearchProjectionWriter searchProjectionWriter)
    {
        this.parkItemRepository = parkItemRepository;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    public async Task<ApplicationResult<BulkAdministrationUpdateResult>> HandleAsync(UpdateParkItemsBulkAdministrationCommand command, CancellationToken cancellationToken = default)
    {
        List<string> normalizedParkItemIds = command.ParkItemIds
            .Where(static parkItemId => !string.IsNullOrWhiteSpace(parkItemId))
            .Select(static parkItemId => parkItemId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedParkItemIds.Count == 0)
        {
            return ApplicationResult<BulkAdministrationUpdateResult>.Failure(ApplicationErrors.Required(nameof(command.ParkItemIds)));
        }

        if (!command.IsVisible.HasValue && !command.AdminReviewStatus.HasValue)
        {
            return ApplicationResult<BulkAdministrationUpdateResult>.Failure(ApplicationErrors.Required("bulkAction"));
        }

        int updatedCount = await this.parkItemRepository.UpdateBulkAdministrationAsync(
            normalizedParkItemIds,
            command.IsVisible,
            command.AdminReviewStatus,
            cancellationToken);

        foreach (string parkItemId in normalizedParkItemIds)
        {
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.ParkItems, parkItemId, cancellationToken);
        }

        return ApplicationResult<BulkAdministrationUpdateResult>.Success(new BulkAdministrationUpdateResult
        {
            RequestedCount = normalizedParkItemIds.Count,
            UpdatedCount = updatedCount,
        });
    }
}
