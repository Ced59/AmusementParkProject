using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Commands;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Handlers;

public sealed class UpdateParkItemsVisibilityByParkIdsCommandHandler : ICommandHandler<UpdateParkItemsVisibilityByParkIdsCommand, ApplicationResult<BulkAdministrationUpdateResult>>
{
    private readonly IParkItemRepository parkItemRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;
    private readonly IPublicSeoUpdateNotifier publicSeoUpdateNotifier;

    public UpdateParkItemsVisibilityByParkIdsCommandHandler(
        IParkItemRepository parkItemRepository,
        ISearchProjectionWriter searchProjectionWriter,
        IPublicSeoUpdateNotifier publicSeoUpdateNotifier)
    {
        this.parkItemRepository = parkItemRepository;
        this.searchProjectionWriter = searchProjectionWriter;
        this.publicSeoUpdateNotifier = publicSeoUpdateNotifier;
    }

    public async Task<ApplicationResult<BulkAdministrationUpdateResult>> HandleAsync(UpdateParkItemsVisibilityByParkIdsCommand command, CancellationToken cancellationToken = default)
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

        IReadOnlyCollection<ParkItem> previousItems = await this.parkItemRepository.GetByParkIdsAsync(normalizedParkIds, true, cancellationToken);
        List<string> parkItemIds = previousItems
            .Select(static item => item.Id)
            .Where(static parkItemId => !string.IsNullOrWhiteSpace(parkItemId))
            .Select(static parkItemId => parkItemId!)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (parkItemIds.Count == 0)
        {
            return ApplicationResult<BulkAdministrationUpdateResult>.Success(new BulkAdministrationUpdateResult
            {
                RequestedCount = 0,
                UpdatedCount = 0,
            });
        }

        if (command.IsVisible)
        {
            ApplicationError? publicationError = this.ValidateBulkPublication(previousItems);
            if (publicationError is not null)
            {
                return ApplicationResult<BulkAdministrationUpdateResult>.Failure(publicationError);
            }
        }

        int updatedCount = await this.parkItemRepository.UpdateBulkAdministrationAsync(
            parkItemIds,
            command.IsVisible,
            null,
            cancellationToken);

        await this.searchProjectionWriter.UpsertManyAsync(SearchProjectionResourceTypes.ParkItems, parkItemIds, cancellationToken);

        if (updatedCount > 0)
        {
            IReadOnlyCollection<ParkItem> currentItems = await this.parkItemRepository.GetByIdsAsync(parkItemIds, cancellationToken);
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
            RequestedCount = parkItemIds.Count,
            UpdatedCount = updatedCount,
        });
    }

    private ApplicationError? ValidateBulkPublication(IReadOnlyCollection<ParkItem> items)
    {
        List<string> missingRequirementKeys = items
            .Select(static item => item.EvaluateContentQuality())
            .Where(static quality => !quality.IsPublishable)
            .SelectMany(static quality => quality.MissingRequirementKeys)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return missingRequirementKeys.Count == 0 ? null : ParkItemApplicationErrors.PublicationBlocked(missingRequirementKeys);
    }
}
