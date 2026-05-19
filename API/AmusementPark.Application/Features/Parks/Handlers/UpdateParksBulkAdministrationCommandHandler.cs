using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Commands;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler d'action de masse d'administration sur les parcs.
/// </summary>
public sealed class UpdateParksBulkAdministrationCommandHandler : ICommandHandler<UpdateParksBulkAdministrationCommand, ApplicationResult<BulkAdministrationUpdateResult>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    public UpdateParksBulkAdministrationCommandHandler(IParkRepository parkRepository, IParkItemRepository parkItemRepository, ISearchProjectionWriter searchProjectionWriter)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    public async Task<ApplicationResult<BulkAdministrationUpdateResult>> HandleAsync(UpdateParksBulkAdministrationCommand command, CancellationToken cancellationToken = default)
    {
        List<string> normalizedParkIds = command.ParkIds
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedParkIds.Count == 0)
        {
            return ApplicationResult<BulkAdministrationUpdateResult>.Failure(ApplicationErrors.Required(nameof(command.ParkIds)));
        }

        if (!command.IsVisible.HasValue && !command.AdminReviewStatus.HasValue)
        {
            return ApplicationResult<BulkAdministrationUpdateResult>.Failure(ApplicationErrors.Required("bulkAction"));
        }

        int updatedCount = await this.parkRepository.UpdateBulkAdministrationAsync(
            normalizedParkIds,
            command.IsVisible,
            command.AdminReviewStatus,
            cancellationToken);

        foreach (string parkId in normalizedParkIds)
        {
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, parkId, cancellationToken);
            IReadOnlyCollection<ParkItem> parkItems = await this.parkItemRepository.GetByParkIdAsync(parkId, true, cancellationToken);
            foreach (ParkItem parkItem in parkItems)
            {
                await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.ParkItems, parkItem.Id, cancellationToken);
            }
        }

        return ApplicationResult<BulkAdministrationUpdateResult>.Success(new BulkAdministrationUpdateResult
        {
            RequestedCount = normalizedParkIds.Count,
            UpdatedCount = updatedCount,
        });
    }
}
