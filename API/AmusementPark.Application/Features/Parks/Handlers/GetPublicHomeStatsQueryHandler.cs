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

        Task<long> parksCountTask = this.parkRepository.CountAsync(includeHidden, cancellationToken);
        Task<long> attractionsCountTask = this.parkItemRepository.CountByCategoryAsync(
            ParkItemCategory.Attraction,
            includeHidden,
            cancellationToken);
        Task<int> countriesCountTask = this.parkRepository.CountDistinctCountryCodesAsync(includeHidden, cancellationToken);

        await Task.WhenAll(parksCountTask, attractionsCountTask, countriesCountTask);

        long parksCount = await parksCountTask;
        long attractionsCount = await attractionsCountTask;
        int countriesCount = await countriesCountTask;

        PublicHomeStatsResult result = new PublicHomeStatsResult(parksCount, attractionsCount, countriesCount);
        return ApplicationResult<PublicHomeStatsResult>.Success(result);
    }
}
