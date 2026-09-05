using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Handlers;

internal static class HomeParkCardResultBuilder
{
    public static async Task<IReadOnlyCollection<HomeFeaturedParkResult>> BuildAsync(
        IReadOnlyCollection<Park> parks,
        IParkItemRepository parkItemRepository,
        CancellationToken cancellationToken)
    {
        List<string> parkIds = parks
            .Where(static park => !string.IsNullOrWhiteSpace(park.Id))
            .Select(static park => park.Id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        IReadOnlyDictionary<string, IReadOnlyDictionary<ParkItemCategory, int>> countsByParkId =
            await parkItemRepository.GetCountsByCategoryForParkIdsAsync(
                parkIds,
                includeHidden: false,
                ClosedEntityFilter.OpenOnly,
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
}
