using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

/// <summary>
/// Handler d'action de masse sur les champs metier rapides des park items.
/// </summary>
public sealed class UpdateParkItemsBulkFieldsCommandHandler : ICommandHandler<UpdateParkItemsBulkFieldsCommand, ApplicationResult<BulkAdministrationUpdateResult>>
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    public UpdateParkItemsBulkFieldsCommandHandler(IParkItemRepository parkItemRepository, ISearchProjectionWriter searchProjectionWriter)
    {
        this.parkItemRepository = parkItemRepository;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    public async Task<ApplicationResult<BulkAdministrationUpdateResult>> HandleAsync(UpdateParkItemsBulkFieldsCommand command, CancellationToken cancellationToken = default)
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

        if (!HasBulkFieldAction(command))
        {
            return ApplicationResult<BulkAdministrationUpdateResult>.Failure(ApplicationErrors.Required("bulkFieldAction"));
        }

        int updatedCount = await this.parkItemRepository.UpdateBulkFieldsAsync(
            normalizedParkItemIds,
            command.UpdateZone,
            NormalizeNullableText(command.ZoneId),
            command.Category,
            command.Type,
            command.UpdateManufacturer,
            NormalizeNullableText(command.ManufacturerId),
            command.IsVisible,
            command.AdminReviewStatus,
            cancellationToken);

        await this.searchProjectionWriter.UpsertManyAsync(SearchProjectionResourceTypes.ParkItems, normalizedParkItemIds, cancellationToken);

        return ApplicationResult<BulkAdministrationUpdateResult>.Success(new BulkAdministrationUpdateResult
        {
            RequestedCount = normalizedParkItemIds.Count,
            UpdatedCount = updatedCount,
        });
    }

    private static bool HasBulkFieldAction(UpdateParkItemsBulkFieldsCommand command)
    {
        return command.UpdateZone
            || command.Category.HasValue
            || command.Type.HasValue
            || command.UpdateManufacturer
            || command.IsVisible.HasValue
            || command.AdminReviewStatus.HasValue;
    }

    private static string? NormalizeNullableText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
