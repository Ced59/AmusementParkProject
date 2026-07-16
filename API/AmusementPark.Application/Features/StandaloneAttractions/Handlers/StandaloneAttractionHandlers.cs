using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Commands;
using AmusementPark.Application.Features.StandaloneAttractions.Contracts;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Queries;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.StandaloneAttractions.Handlers;

public sealed class GetStandaloneAttractionByIdQueryHandler
    : IQueryHandler<GetStandaloneAttractionByIdQuery, ApplicationResult<StandaloneAttraction>>
{
    private readonly IStandaloneAttractionRepository repository;

    public GetStandaloneAttractionByIdQueryHandler(IStandaloneAttractionRepository repository)
    {
        this.repository = repository;
    }

    public async Task<ApplicationResult<StandaloneAttraction>> HandleAsync(GetStandaloneAttractionByIdQuery query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Id))
        {
            return ApplicationResult<StandaloneAttraction>.Failure(ApplicationErrors.Required(nameof(query.Id)));
        }

        StandaloneAttraction? attraction = await this.repository.GetByIdAsync(query.Id.Trim(), query.IncludeHidden, cancellationToken);
        return attraction is null
            ? ApplicationResult<StandaloneAttraction>.Failure(ApplicationErrors.EntityNotFound(nameof(StandaloneAttraction), query.Id))
            : ApplicationResult<StandaloneAttraction>.Success(attraction);
    }
}

public sealed class GetStandaloneAttractionsPageQueryHandler
    : IQueryHandler<GetStandaloneAttractionsPageQuery, ApplicationResult<PagedResult<StandaloneAttraction>>>
{
    private readonly IStandaloneAttractionRepository repository;
    private readonly IApplicationValidator<PagedQuery> pagedQueryValidator;

    public GetStandaloneAttractionsPageQueryHandler(
        IStandaloneAttractionRepository repository,
        IApplicationValidator<PagedQuery> pagedQueryValidator)
    {
        this.repository = repository;
        this.pagedQueryValidator = pagedQueryValidator;
    }

    public async Task<ApplicationResult<PagedResult<StandaloneAttraction>>> HandleAsync(GetStandaloneAttractionsPageQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ApplicationError> validationErrors = this.pagedQueryValidator.Validate(query.Paging);
        if (validationErrors.Count > 0)
        {
            return ApplicationResult<PagedResult<StandaloneAttraction>>.Failure(validationErrors);
        }

        PagedResult<StandaloneAttraction> page = await this.repository.GetPageAsync(
            query.Paging.Page,
            query.Paging.PageSize,
            query.Search,
            query.IncludeHidden,
            query.IsVisible,
            query.AdminReviewStatus,
            query.Type,
            query.CountryCode,
            query.ManufacturerId,
            cancellationToken,
            query.SortField,
            query.SortDescending);

        return ApplicationResult<PagedResult<StandaloneAttraction>>.Success(page);
    }
}

public sealed class CreateStandaloneAttractionCommandHandler
    : ICommandHandler<CreateStandaloneAttractionCommand, ApplicationResult<StandaloneAttraction>>
{
    private readonly IStandaloneAttractionRepository repository;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    public CreateStandaloneAttractionCommandHandler(IStandaloneAttractionRepository repository, ISearchProjectionWriter searchProjectionWriter)
    {
        this.repository = repository;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    public async Task<ApplicationResult<StandaloneAttraction>> HandleAsync(CreateStandaloneAttractionCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Attraction is null)
        {
            return ApplicationResult<StandaloneAttraction>.Failure(ApplicationErrors.Required(nameof(command.Attraction)));
        }

        Normalize(command.Attraction);
        if (string.IsNullOrWhiteSpace(command.Attraction.Name))
        {
            return ApplicationResult<StandaloneAttraction>.Failure(ApplicationErrors.Required(nameof(command.Attraction.Name)));
        }

        StandaloneAttraction created = await this.repository.CreateAsync(command.Attraction, cancellationToken);
        if (!string.IsNullOrWhiteSpace(created.Id))
        {
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.StandaloneAttractions, created.Id, cancellationToken);
        }

        return ApplicationResult<StandaloneAttraction>.Success(created);
    }

    internal static void Normalize(StandaloneAttraction attraction)
    {
        attraction.Name = attraction.Name.Trim();
        attraction.CountryCode = NormalizeUpper(attraction.CountryCode);
        attraction.OperatorId = NormalizeOptional(attraction.OperatorId);
        attraction.WebsiteUrl = NormalizeOptional(attraction.WebsiteUrl);
        attraction.Street = NormalizeOptional(attraction.Street);
        attraction.City = NormalizeOptional(attraction.City);
        attraction.PostalCode = NormalizeOptional(attraction.PostalCode);
        attraction.Subtype = NormalizeOptional(attraction.Subtype);
        attraction.LegacyParkId = NormalizeOptional(attraction.LegacyParkId);
        attraction.LegacyParkItemId = NormalizeOptional(attraction.LegacyParkItemId);
        attraction.AdminReviewStatus = NormalizeAdminReviewStatus(attraction.AdminReviewStatus);
        attraction.Type = attraction.Type == ParkItemType.Other ? ParkItemType.Attraction : attraction.Type;
        if (attraction.Type == ParkItemType.Attraction)
        {
            attraction.AttractionDetails ??= new AttractionDetails();
        }
    }

    private static string? NormalizeUpper(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static AdminReviewStatus NormalizeAdminReviewStatus(AdminReviewStatus value)
    {
        return value == AdminReviewStatus.Ready ? AdminReviewStatus.Validated : value;
    }
}

public sealed class UpdateStandaloneAttractionCommandHandler
    : ICommandHandler<UpdateStandaloneAttractionCommand, ApplicationResult<StandaloneAttraction>>
{
    private readonly IStandaloneAttractionRepository repository;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    public UpdateStandaloneAttractionCommandHandler(IStandaloneAttractionRepository repository, ISearchProjectionWriter searchProjectionWriter)
    {
        this.repository = repository;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    public async Task<ApplicationResult<StandaloneAttraction>> HandleAsync(UpdateStandaloneAttractionCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Id))
        {
            return ApplicationResult<StandaloneAttraction>.Failure(ApplicationErrors.Required(nameof(command.Id)));
        }

        if (command.Attraction is null)
        {
            return ApplicationResult<StandaloneAttraction>.Failure(ApplicationErrors.Required(nameof(command.Attraction)));
        }

        CreateStandaloneAttractionCommandHandler.Normalize(command.Attraction);
        StandaloneAttraction? updated = await this.repository.UpdateAsync(command.Id.Trim(), command.Attraction, cancellationToken);
        if (updated is null)
        {
            return ApplicationResult<StandaloneAttraction>.Failure(ApplicationErrors.EntityNotFound(nameof(StandaloneAttraction), command.Id));
        }

        if (!string.IsNullOrWhiteSpace(updated.Id))
        {
            await this.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.StandaloneAttractions, updated.Id, cancellationToken);
        }

        return ApplicationResult<StandaloneAttraction>.Success(updated);
    }
}

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
