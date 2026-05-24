using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Countries.Ports;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de récupération des points de carte visibles publiquement.
/// </summary>
public sealed class GetVisibleParkMapPointsQueryHandler : IQueryHandler<GetVisibleParkMapPointsQuery, ApplicationResult<IReadOnlyCollection<Park>>>
{
    private readonly IParkRepository parkRepository;
    private readonly ICountryReferenceService countryReferenceService;

    public GetVisibleParkMapPointsQueryHandler(IParkRepository parkRepository, ICountryReferenceService countryReferenceService)
    {
        this.parkRepository = parkRepository;
        this.countryReferenceService = countryReferenceService;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<Park>>> HandleAsync(GetVisibleParkMapPointsQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<string> matchingCountryCodes = await this.countryReferenceService.FindCountryCodesByLocalizedSearchAsync(query.SearchTerm, cancellationToken);
        IReadOnlyCollection<string> regionCountryCodes = this.countryReferenceService.GetCountryCodesForRegion(query.Region);
        ParkSearchCriteria criteria = new ParkSearchCriteria(query.SearchTerm, matchingCountryCodes, regionCountryCodes);

        IReadOnlyCollection<Park> parks = await this.parkRepository.GetVisibleMapPointsAsync(criteria, cancellationToken);
        return ApplicationResult<IReadOnlyCollection<Park>>.Success(parks);
    }
}
