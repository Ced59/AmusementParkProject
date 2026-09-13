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

public sealed class MigrateParkToStandaloneAttractionCommandHandler
    : ICommandHandler<MigrateParkToStandaloneAttractionCommand, ApplicationResult<StandaloneAttraction>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IStandaloneAttractionRepository standaloneAttractionRepository;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    public MigrateParkToStandaloneAttractionCommandHandler(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IStandaloneAttractionRepository standaloneAttractionRepository,
        ISearchProjectionWriter searchProjectionWriter)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.standaloneAttractionRepository = standaloneAttractionRepository;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    public async Task<ApplicationResult<StandaloneAttraction>> HandleAsync(MigrateParkToStandaloneAttractionCommand command, CancellationToken cancellationToken = default)
    {
        StandaloneAttractionMigrationRequest request = command.Request;
        if (request is null || string.IsNullOrWhiteSpace(request.LegacyParkId))
        {
            return ApplicationResult<StandaloneAttraction>.Failure(ApplicationErrors.Required(nameof(request.LegacyParkId)));
        }

        Park? sourcePark = await this.parkRepository.GetByIdAsync(request.LegacyParkId.Trim(), true, cancellationToken);
        if (sourcePark is null)
        {
            return ApplicationResult<StandaloneAttraction>.Failure(ApplicationErrors.EntityNotFound(nameof(Park), request.LegacyParkId));
        }

        ParkItem? sourceItem = await this.ResolveSourceItemAsync(sourcePark.Id, request.LegacyParkItemId, cancellationToken);
        if (!string.IsNullOrWhiteSpace(request.LegacyParkItemId) && sourceItem is null)
        {
            return ApplicationResult<StandaloneAttraction>.Failure(ApplicationErrors.EntityNotFound(nameof(ParkItem), request.LegacyParkItemId));
        }

        StandaloneAttraction? existing = !string.IsNullOrWhiteSpace(request.TargetStandaloneAttractionId)
            ? await this.standaloneAttractionRepository.GetByIdAsync(request.TargetStandaloneAttractionId.Trim(), true, cancellationToken)
            : await this.standaloneAttractionRepository.FindByLegacyAsync(sourcePark.Id, sourceItem?.Id, cancellationToken);

        StandaloneAttraction attraction = BuildStandaloneAttraction(sourcePark, sourceItem);
        if (!string.IsNullOrWhiteSpace(request.TargetStandaloneAttractionId))
        {
            attraction.Id = request.TargetStandaloneAttractionId.Trim();
        }
        else if (existing is not null)
        {
            attraction.Id = existing.Id;
        }

        CreateStandaloneAttractionCommandHandler.Normalize(attraction);
        StandaloneAttraction saved = existing is null && string.IsNullOrWhiteSpace(attraction.Id)
            ? await this.standaloneAttractionRepository.CreateAsync(attraction, cancellationToken)
            : await this.UpsertWithIdAsync(attraction, cancellationToken);

        await this.RetireLegacyEntitiesAsync(sourcePark, sourceItem, request, cancellationToken);
        if (!string.IsNullOrWhiteSpace(saved.Id))
        {
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.StandaloneAttractions, saved.Id, cancellationToken);
        }

        await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Parks, sourcePark.Id, cancellationToken);
        if (!string.IsNullOrWhiteSpace(sourceItem?.Id))
        {
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.ParkItems, sourceItem.Id, cancellationToken);
        }

        return ApplicationResult<StandaloneAttraction>.Success(saved);
    }

    private async Task<StandaloneAttraction> UpsertWithIdAsync(StandaloneAttraction attraction, CancellationToken cancellationToken)
    {
        StandaloneAttraction? existing = string.IsNullOrWhiteSpace(attraction.Id)
            ? null
            : await this.standaloneAttractionRepository.GetByIdAsync(attraction.Id, true, cancellationToken);

        if (existing is null)
        {
            return await this.standaloneAttractionRepository.CreateAsync(attraction, cancellationToken);
        }

        StandaloneAttraction? updated = await this.standaloneAttractionRepository.UpdateAsync(attraction.Id, attraction, cancellationToken);
        return updated ?? attraction;
    }

    private async Task<ParkItem?> ResolveSourceItemAsync(string? parkId, string? legacyParkItemId, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(legacyParkItemId))
        {
            ParkItem? item = await this.parkItemRepository.GetByIdAsync(legacyParkItemId.Trim(), true, cancellationToken);
            return item is not null && string.Equals(item.ParkId, parkId, StringComparison.Ordinal) ? item : null;
        }

        if (string.IsNullOrWhiteSpace(parkId))
        {
            return null;
        }

        IReadOnlyCollection<ParkItem> items = await this.parkItemRepository.GetByParkIdAsync(parkId, true, cancellationToken);
        return items.Count == 1 ? items.First() : null;
    }

    private static StandaloneAttraction BuildStandaloneAttraction(Park sourcePark, ParkItem? sourceItem)
    {
        StandaloneAttraction attraction = new StandaloneAttraction
        {
            Name = sourceItem?.Name ?? sourcePark.Name ?? string.Empty,
            CountryCode = sourcePark.CountryCode,
            Type = sourceItem?.Type ?? ParkItemType.Attraction,
            Subtype = sourceItem?.Subtype,
            OperatorId = sourcePark.OperatorId,
            WebsiteUrl = sourcePark.WebsiteUrl,
            Street = sourcePark.Street,
            City = sourcePark.City,
            PostalCode = sourcePark.PostalCode,
            Descriptions = sourceItem?.Descriptions.Count > 0 ? sourceItem.Descriptions.ToList() : sourcePark.Descriptions.ToList(),
            AttractionDetails = sourceItem?.AttractionDetails,
            AttractionLocations = sourceItem?.AttractionLocations,
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
            LegacyParkId = sourcePark.Id,
            LegacyParkItemId = sourceItem?.Id,
        };

        if (sourceItem?.Position is not null)
        {
            attraction.SetPosition(sourceItem.Position.Latitude, sourceItem.Position.Longitude);
        }
        else if (sourcePark.Position is not null)
        {
            attraction.SetPosition(sourcePark.Position.Latitude, sourcePark.Position.Longitude);
        }

        return attraction;
    }

    private async Task RetireLegacyEntitiesAsync(
        Park sourcePark,
        ParkItem? sourceItem,
        StandaloneAttractionMigrationRequest request,
        CancellationToken cancellationToken)
    {
        if (request.RetireLegacyPark)
        {
            sourcePark.IsVisible = false;
            sourcePark.AdminReviewStatus = AdminReviewStatus.NotRelevant;
            await this.parkRepository.UpdateAsync(sourcePark.Id, sourcePark, cancellationToken);
        }

        if (request.RetireLegacyParkItem && sourceItem is not null)
        {
            sourceItem.IsVisible = false;
            sourceItem.AdminReviewStatus = AdminReviewStatus.NotRelevant;
            await this.parkItemRepository.UpdateAsync(sourceItem.Id, sourceItem, cancellationToken);
        }
    }
}
