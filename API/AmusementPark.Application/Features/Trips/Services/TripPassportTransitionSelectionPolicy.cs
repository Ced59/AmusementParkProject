using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Trips.Services;

internal static class TripPassportTransitionSelectionPolicy
{
    public static IReadOnlySet<string> ResolveSelectableIds(
        IEnumerable<ParkItem> currentItems,
        PassportHistoricalTargetContext? historicalContext)
    {
        ArgumentNullException.ThrowIfNull(currentItems);
        IReadOnlyDictionary<string, PassportHistoricalTarget> historicalTargets =
            historicalContext?.Targets
            ?? new Dictionary<string, PassportHistoricalTarget>(StringComparer.Ordinal);
        return currentItems
            .Where(static item => item.Category == ParkItemCategory.Attraction
                && item.IsVisible
                && !string.IsNullOrWhiteSpace(item.Id))
            .Select(static item => item.Id!)
            .Concat(historicalTargets.Values
                .Where(static target =>
                    string.Equals(
                        target.Category,
                        ParkItemCategory.Attraction.ToString(),
                        StringComparison.OrdinalIgnoreCase)
                    && target.OperationalState != HistoricalOperationalState.KnownClosed)
                .Select(static target => target.ParkItemId))
            .Distinct(StringComparer.Ordinal)
            .Where(id => historicalContext?.CanonicallyExcludedParkItemIds.Contains(id) != true)
            .Where(id => historicalTargets.GetValueOrDefault(id)?.OperationalState
                != HistoricalOperationalState.KnownClosed)
            .ToHashSet(StringComparer.Ordinal);
    }
}
