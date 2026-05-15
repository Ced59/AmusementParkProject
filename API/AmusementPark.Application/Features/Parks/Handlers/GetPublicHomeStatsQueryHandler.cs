using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de récupération des statistiques publiques affichées sur la home.
/// </summary>
public sealed class GetPublicHomeStatsQueryHandler : IQueryHandler<GetPublicHomeStatsQuery, ApplicationResult<PublicHomeStatsResult>>
{
    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;

    public GetPublicHomeStatsQueryHandler(IParkRepository parkRepository, IParkItemRepository parkItemRepository)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
    }

    public async Task<ApplicationResult<PublicHomeStatsResult>> HandleAsync(GetPublicHomeStatsQuery query, CancellationToken cancellationToken = default)
    {
        const bool includeHidden = false;

        IReadOnlyCollection<string> visibleParkIds = await this.parkRepository.GetVisibleParkIdsAsync(cancellationToken);
        long parksCount = visibleParkIds.Count;
        long attractionsCount = await this.parkItemRepository.CountByCategoryForParkIdsAsync(
            ParkItemCategory.Attraction,
            visibleParkIds,
            includeHidden,
            cancellationToken);
        int countriesCount = await this.parkRepository.CountDistinctCountryCodesAsync(includeHidden, cancellationToken);

        PublicHomeStatsResult result = new PublicHomeStatsResult(parksCount, attractionsCount, countriesCount);
        return ApplicationResult<PublicHomeStatsResult>.Success(result);
    }
}
