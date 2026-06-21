using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Application.Features.ParkItems.Services;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

/// <summary>
/// Handler d'action de masse d'administration sur les park items.
/// </summary>
public sealed class UpdateParkItemsBulkAdministrationCommandHandler : ICommandHandler<UpdateParkItemsBulkAdministrationCommand, ApplicationResult<BulkAdministrationUpdateResult>>
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;
    private readonly ParkItemContentQualityService contentQualityService;
    private readonly IPublicSeoUpdateNotifier publicSeoUpdateNotifier;

    public UpdateParkItemsBulkAdministrationCommandHandler(
        IParkItemRepository parkItemRepository,
        ISearchProjectionWriter searchProjectionWriter,
        ParkItemContentQualityService contentQualityService,
        IPublicSeoUpdateNotifier publicSeoUpdateNotifier)
    {
        this.parkItemRepository = parkItemRepository;
        this.searchProjectionWriter = searchProjectionWriter;
        this.contentQualityService = contentQualityService;
        this.publicSeoUpdateNotifier = publicSeoUpdateNotifier;
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

        if (command.IsVisible == true)
        {
            ApplicationError? publicationError = await this.ValidateBulkPublicationAsync(normalizedParkItemIds, cancellationToken);
            if (publicationError is not null)
            {
                return ApplicationResult<BulkAdministrationUpdateResult>.Failure(publicationError);
            }
        }

        IReadOnlyCollection<ParkItem> previousItems = await this.parkItemRepository.GetByIdsAsync(normalizedParkItemIds, cancellationToken);

        int updatedCount = await this.parkItemRepository.UpdateBulkAdministrationAsync(
            normalizedParkItemIds,
            command.IsVisible,
            command.AdminReviewStatus,
            cancellationToken);

        await this.searchProjectionWriter.UpsertManyAsync(SearchProjectionResourceTypes.ParkItems, normalizedParkItemIds, cancellationToken);

        if (updatedCount > 0)
        {
            IReadOnlyCollection<ParkItem> currentItems = await this.parkItemRepository.GetByIdsAsync(normalizedParkItemIds, cancellationToken);
            await this.publicSeoUpdateNotifier.NotifyAsync(
                new PublicSeoUpdate
                {
                    PreviousParkItems = PublicSeoParkItemSnapshot.FromParkItems(previousItems),
                    CurrentParkItems = PublicSeoParkItemSnapshot.FromParkItems(currentItems),
                },
                cancellationToken);
        }

        return ApplicationResult<BulkAdministrationUpdateResult>.Success(new BulkAdministrationUpdateResult
        {
            RequestedCount = normalizedParkItemIds.Count,
            UpdatedCount = updatedCount,
        });
    }

    private async Task<ApplicationError?> ValidateBulkPublicationAsync(IReadOnlyCollection<string> parkItemIds, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkItem> items = await this.parkItemRepository.GetByIdsAsync(parkItemIds, cancellationToken);
        List<string> missingRequirementKeys = items
            .Select(this.contentQualityService.Evaluate)
            .Where(static quality => !quality.IsPublishable)
            .SelectMany(static quality => quality.MissingRequirementKeys)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return missingRequirementKeys.Count == 0 ? null : ParkItemApplicationErrors.PublicationBlocked(missingRequirementKeys);
    }
}
