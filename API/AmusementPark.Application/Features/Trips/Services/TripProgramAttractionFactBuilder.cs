using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripProgramAttractionFactBuilder
{
    public IReadOnlyCollection<TripProgramAttractionFact> Build(
        IReadOnlyCollection<TripItemDecision> decisions,
        IReadOnlyDictionary<string, ParkItem> itemsById,
        IReadOnlyDictionary<string, Park> parksById,
        IReadOnlyCollection<TripPreferenceCount> preferenceCounts)
    {
        ArgumentNullException.ThrowIfNull(decisions);
        ArgumentNullException.ThrowIfNull(itemsById);
        ArgumentNullException.ThrowIfNull(parksById);
        ArgumentNullException.ThrowIfNull(preferenceCounts);
        Dictionary<string, TripPreferenceCount> constraintsByItem = preferenceCounts
            .Where(static count => count.Level == TripItemPreferenceLevel.NotForMe)
            .ToDictionary(static count => count.ParkItemId, StringComparer.Ordinal);

        return decisions.Select(decision =>
        {
            itemsById.TryGetValue(decision.ParkItemId, out ParkItem? item);
            bool itemAvailable = IsAvailable(item, parksById);
            TripPreferenceCount? constraint = constraintsByItem.GetValueOrDefault(decision.ParkItemId);
            return new TripProgramAttractionFact(
                decision.ParkItemId,
                item?.ParkId ?? string.Empty,
                decision.Status,
                itemAvailable,
                ParkItemStatusNormalizer.Normalize(item?.AttractionDetails?.Status),
                constraint?.Count ?? 0,
                constraint?.LatestUpdatedAtUtc,
                decision.UpdatedAtUtc);
        }).ToArray();
    }

    internal static bool IsAvailable(
        ParkItem? item,
        IReadOnlyDictionary<string, Park> parksById)
    {
        return item is not null
            && item.IsVisible
            && item.AdminReviewStatus != AdminReviewStatus.NotRelevant
            && parksById.TryGetValue(item.ParkId, out Park? park)
            && park.IsPubliclyDiscoverable();
    }
}
