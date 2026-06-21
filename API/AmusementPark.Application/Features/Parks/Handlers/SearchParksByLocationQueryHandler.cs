using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de recherche de parcs par position.
/// </summary>
public sealed class SearchParksByLocationQueryHandler : IQueryHandler<SearchParksByLocationQuery, ApplicationResult<IReadOnlyCollection<Park>>>
{
    private readonly IParkRepository parkRepository;

    public SearchParksByLocationQueryHandler(IParkRepository parkRepository)
    {
        this.parkRepository = parkRepository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<Park>>> HandleAsync(SearchParksByLocationQuery query, CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<Park> parks = await this.parkRepository.SearchByLocationAsync(query.Latitude, query.Longitude, query.RadiusInKilometers, query.IncludeHidden, query.ClosedFilter, cancellationToken);
        if (parks.Count == 0)
        {
            return ApplicationResult<IReadOnlyCollection<Park>>.Failure(ParkApplicationErrors.NoParkInThisLocation());
        }

        return ApplicationResult<IReadOnlyCollection<Park>>.Success(parks);
    }
}
