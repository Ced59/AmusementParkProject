using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

/// <summary>
/// Handler de récupération des parcs à la une de la home publique.
/// </summary>
public sealed class GetHomeFeaturedParksQueryHandler : IQueryHandler<GetHomeFeaturedParksQuery, ApplicationResult<IReadOnlyCollection<HomeFeaturedParkResult>>>
{
    private const int DefaultLimit = 3;
    private const int MinimumLimit = 1;
    private const int MaximumLimit = 3;

    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;

    public GetHomeFeaturedParksQueryHandler(IParkRepository parkRepository, IParkItemRepository parkItemRepository)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
    }

    public async Task<ApplicationResult<IReadOnlyCollection<HomeFeaturedParkResult>>> HandleAsync(
        GetHomeFeaturedParksQuery query,
        CancellationToken cancellationToken = default)
    {
        int requestedLimit = query.Limit <= 0 ? DefaultLimit : query.Limit;
        int normalizedLimit = Math.Clamp(requestedLimit, MinimumLimit, MaximumLimit);
        List<string> excludedParkIds = NormalizeParkIds(query.ExcludedParkIds);

        IReadOnlyCollection<Park> manualParks = await this.parkRepository.GetManualHomeFeaturedVisibleAsync(
            normalizedLimit,
            Array.Empty<string>(),
            cancellationToken);

        List<Park> selectedParks = new List<Park>(normalizedLimit);
        selectedParks.AddRange(manualParks);

        int remainingSlots = normalizedLimit - selectedParks.Count;

        if (remainingSlots > 0)
        {
            List<string> randomExcludedParkIds = excludedParkIds
                .Concat(manualParks.Select(static park => park.Id))
                .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
                .Select(static parkId => parkId.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            IReadOnlyCollection<Park> randomParks = await this.parkRepository.GetRandomVisibleAsync(
                remainingSlots,
                randomExcludedParkIds,
                cancellationToken);

            selectedParks.AddRange(randomParks);
        }

        List<Park> normalizedSelection = selectedParks
            .Take(normalizedLimit)
            .ToList();

        IReadOnlyCollection<HomeFeaturedParkResult> results = await this.BuildResultsAsync(
            normalizedSelection,
            cancellationToken);

        return ApplicationResult<IReadOnlyCollection<HomeFeaturedParkResult>>.Success(results);
    }

    private async Task<IReadOnlyCollection<HomeFeaturedParkResult>> BuildResultsAsync(
        IReadOnlyCollection<Park> parks,
        CancellationToken cancellationToken)
    {
        List<string> parkIds = parks
            .Where(static park => !string.IsNullOrWhiteSpace(park.Id))
            .Select(static park => park.Id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        IReadOnlyDictionary<string, IReadOnlyDictionary<ParkItemCategory, int>> countsByParkId =
            await this.parkItemRepository.GetCountsByCategoryForParkIdsAsync(
                parkIds,
                includeHidden: false,
                cancellationToken);

        List<HomeFeaturedParkResult> results = new List<HomeFeaturedParkResult>(parks.Count);

        foreach (Park park in parks)
        {
            IReadOnlyDictionary<ParkItemCategory, int> countsByCategory =
                !string.IsNullOrWhiteSpace(park.Id)
                && countsByParkId.TryGetValue(park.Id, out IReadOnlyDictionary<ParkItemCategory, int>? counts)
                    ? counts
                    : new Dictionary<ParkItemCategory, int>();

            results.Add(new HomeFeaturedParkResult(
                park,
                countsByCategory,
                park.IsFeaturedOnHome));
        }

        return results;
    }

    private static List<string> NormalizeParkIds(IEnumerable<string>? parkIds)
    {
        return (parkIds ?? Array.Empty<string>())
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
