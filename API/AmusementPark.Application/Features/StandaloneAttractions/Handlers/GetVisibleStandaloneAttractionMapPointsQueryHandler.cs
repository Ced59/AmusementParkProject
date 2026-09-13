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

public sealed class GetVisibleStandaloneAttractionMapPointsQueryHandler
    : IQueryHandler<GetVisibleStandaloneAttractionMapPointsQuery, ApplicationResult<IReadOnlyCollection<StandaloneAttraction>>>
{
    private readonly IStandaloneAttractionRepository repository;
    private readonly ICountryReferenceService countryReferenceService;

    public GetVisibleStandaloneAttractionMapPointsQueryHandler(
        IStandaloneAttractionRepository repository,
        ICountryReferenceService countryReferenceService)
    {
        this.repository = repository;
        this.countryReferenceService = countryReferenceService;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<StandaloneAttraction>>> HandleAsync(
        GetVisibleStandaloneAttractionMapPointsQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string> matchingCountryCodes = await this.countryReferenceService.FindCountryCodesByLocalizedSearchAsync(
            query.SearchTerm,
            cancellationToken);
        IReadOnlyCollection<string> regionCountryCodes = this.countryReferenceService.GetCountryCodesForRegion(query.Region);
        StandaloneAttractionSearchCriteria criteria = new StandaloneAttractionSearchCriteria(
            query.SearchTerm,
            matchingCountryCodes,
            regionCountryCodes);
        IReadOnlyCollection<StandaloneAttraction> attractions = await this.repository.GetVisibleMapPointsAsync(criteria, cancellationToken);
        return ApplicationResult<IReadOnlyCollection<StandaloneAttraction>>.Success(attractions);
    }
}
