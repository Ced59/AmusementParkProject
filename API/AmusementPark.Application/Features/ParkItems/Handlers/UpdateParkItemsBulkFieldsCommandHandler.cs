using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Application.Features.ParkItems.Services;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

/// <summary>
/// Handler d'action de masse sur les champs metier rapides des park items.
/// </summary>
public sealed class UpdateParkItemsBulkFieldsCommandHandler : ICommandHandler<UpdateParkItemsBulkFieldsCommand, ApplicationResult<BulkAdministrationUpdateResult>>
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;
    private readonly ParkItemContentQualityService contentQualityService;

    public UpdateParkItemsBulkFieldsCommandHandler(IParkItemRepository parkItemRepository, ISearchProjectionWriter searchProjectionWriter, ParkItemContentQualityService contentQualityService)
    {
        this.parkItemRepository = parkItemRepository;
        this.searchProjectionWriter = searchProjectionWriter;
        this.contentQualityService = contentQualityService;
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

        if (command.IsVisible == true)
        {
            ApplicationError? publicationError = await this.ValidateBulkPublicationAsync(normalizedParkItemIds, command, cancellationToken);
            if (publicationError is not null)
            {
                return ApplicationResult<BulkAdministrationUpdateResult>.Failure(publicationError);
            }
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

    private async Task<ApplicationError?> ValidateBulkPublicationAsync(IReadOnlyCollection<string> parkItemIds, UpdateParkItemsBulkFieldsCommand command, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkItem> items = await this.parkItemRepository.GetByIdsAsync(parkItemIds, cancellationToken);
        List<string> missingRequirementKeys = items
            .Select(item => ApplyBulkPreview(item, command))
            .Select(this.contentQualityService.Evaluate)
            .Where(static quality => !quality.IsPublishable)
            .SelectMany(static quality => quality.MissingRequirementKeys)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return missingRequirementKeys.Count == 0 ? null : ParkItemApplicationErrors.PublicationBlocked(missingRequirementKeys);
    }

    private static ParkItem ApplyBulkPreview(ParkItem item, UpdateParkItemsBulkFieldsCommand command)
    {
        ParkItem preview = new ParkItem
        {
            Id = item.Id,
            ParkId = item.ParkId,
            ZoneId = command.UpdateZone ? NormalizeNullableText(command.ZoneId) : item.ZoneId,
            Name = item.Name,
            Category = command.Category ?? item.Category,
            Type = command.Type ?? item.Type,
            Subtype = item.Subtype,
            Descriptions = item.Descriptions,
            AttractionDetails = item.AttractionDetails,
            AttractionLocations = item.AttractionLocations,
            IsVisible = command.IsVisible ?? item.IsVisible,
            AdminReviewStatus = command.AdminReviewStatus ?? item.AdminReviewStatus,
        };

        if (item.Position is not null)
        {
            preview.SetPosition(item.Position.Latitude, item.Position.Longitude);
        }

        return preview;
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
