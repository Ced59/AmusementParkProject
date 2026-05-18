using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Validation;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de recherche publique unifiée des parcs.
/// </summary>
public sealed class SearchParksQueryHandler : IQueryHandler<SearchParksQuery, ApplicationResult<PagedResult<Park>>>
{
    private readonly IParkRepository parkRepository;
    private readonly ICountryReferenceService countryReferenceService;
    private readonly PagedQueryValidator pagedQueryValidator;

    public SearchParksQueryHandler(
        IParkRepository parkRepository,
        ICountryReferenceService countryReferenceService,
        PagedQueryValidator pagedQueryValidator)
    {
        this.parkRepository = parkRepository;
        this.countryReferenceService = countryReferenceService;
        this.pagedQueryValidator = pagedQueryValidator;
    }

    public async Task<ApplicationResult<PagedResult<Park>>> HandleAsync(SearchParksQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<ApplicationError> errors = this.pagedQueryValidator.Validate(query.Paging);
        if (errors.Count > 0)
        {
            return ApplicationResult<PagedResult<Park>>.Failure(errors);
        }

        ParkSearchCriteria criteria = await this.BuildCriteriaAsync(query, cancellationToken);
        PagedResult<Park> page = await this.parkRepository.SearchAsync(criteria, query.Paging.Page, query.Paging.PageSize, query.IncludeHidden, cancellationToken);
        return ApplicationResult<PagedResult<Park>>.Success(page);
    }

    private async Task<ParkSearchCriteria> BuildCriteriaAsync(SearchParksQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> matchingCountryCodes = await this.countryReferenceService.FindCountryCodesByLocalizedSearchAsync(query.SearchTerm, cancellationToken);
        IReadOnlyCollection<string> regionCountryCodes = this.countryReferenceService.GetCountryCodesForRegion(query.Region);

        return new ParkSearchCriteria(
            query.SearchTerm,
            matchingCountryCodes,
            regionCountryCodes);
    }
}
