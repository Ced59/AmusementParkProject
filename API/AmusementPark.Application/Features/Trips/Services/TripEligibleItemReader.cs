using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripEligibleItemReader
{
    private readonly ITripParkCandidateRepository candidates;
    private readonly IParkRepository parks;
    private readonly IParkItemRepository parkItems;

    public TripEligibleItemReader(
        ITripParkCandidateRepository candidates,
        IParkRepository parks,
        IParkItemRepository parkItems)
    {
        this.candidates = candidates ?? throw new ArgumentNullException(nameof(candidates));
        this.parks = parks ?? throw new ArgumentNullException(nameof(parks));
        this.parkItems = parkItems ?? throw new ArgumentNullException(nameof(parkItems));
    }

    internal async Task<EligibleTripItems> LoadAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TripParkCandidate> candidates = await this.candidates.ListAsync(
            tripPlanId,
            cancellationToken);
        string[] orderedParkIds = candidates
            .Where(static candidate => candidate.State != TripParkCandidateState.Rejected)
            .OrderBy(static candidate => candidate.SortPosition)
            .Select(static candidate => candidate.ParkId)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyCollection<Park> parks = await this.parks.GetByIdsAsync(
            orderedParkIds,
            cancellationToken);
        Dictionary<string, Park> parksById = parks
            .Where(static park => park.Id is not null && park.IsPubliclyDiscoverable())
            .ToDictionary(static park => park.Id!, StringComparer.Ordinal);
        string[] availableParkIds = orderedParkIds.Where(parksById.ContainsKey).ToArray();
        IReadOnlyCollection<ParkItem> parkItems = await this.parkItems
            .GetVisibleOpenAttractionsByParkIdsAsync(availableParkIds, cancellationToken);
        Dictionary<string, int> parkOrder = availableParkIds
            .Select(static (parkId, index) => new { parkId, index })
            .ToDictionary(static item => item.parkId, static item => item.index, StringComparer.Ordinal);
        ParkItem[] orderedItems = parkItems
            .Where(static item => !string.IsNullOrWhiteSpace(item.Id)
                && !string.IsNullOrWhiteSpace(item.Name))
            .OrderBy(item => parkOrder.GetValueOrDefault(item.ParkId, int.MaxValue))
            .ThenBy(static item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static item => item.Id, StringComparer.Ordinal)
            .ToArray();
        return new EligibleTripItems(
            parksById,
            orderedItems,
            orderedItems.ToDictionary(static item => item.Id, StringComparer.Ordinal));
    }
}
