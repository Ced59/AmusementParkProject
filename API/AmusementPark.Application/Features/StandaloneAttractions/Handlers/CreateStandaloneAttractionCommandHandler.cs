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
