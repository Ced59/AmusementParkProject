using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Seo.Services;

internal static class HistoryTimelineIndexabilityResolver
{
    public static HistoryTimelineIndexability Resolve(HistorySitemapResolvedData resolvedData)
    {
        Dictionary<string, int> parkEventCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> parentParkItemEventCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> parkItemEventCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, int> standaloneAttractionEventCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (HistoryEvent historyEvent in resolvedData.Events)
        {
            if (historyEvent.EntityType == HistoryEntityType.Park)
            {
                if (resolvedData.PublicParkById.ContainsKey(historyEvent.OwnerId))
                {
                    IncrementCount(parkEventCounts, historyEvent.OwnerId);
                }

                continue;
            }

            if (historyEvent.EntityType == HistoryEntityType.StandaloneAttraction)
            {
                if (resolvedData.PublicStandaloneAttractionById.ContainsKey(historyEvent.OwnerId))
                {
                    IncrementCount(standaloneAttractionEventCounts, historyEvent.OwnerId);
                }

                continue;
            }

            if (resolvedData.PublicItemById.TryGetValue(historyEvent.OwnerId, out ParkItem? item) &&
                resolvedData.PublicParkById.ContainsKey(item.ParkId))
            {
                IncrementCount(parkItemEventCounts, historyEvent.OwnerId);
            }

            string? parentParkId = ResolveParentParkId(resolvedData, historyEvent);
            if (parentParkId is not null)
            {
                IncrementCount(parentParkItemEventCounts, parentParkId);
            }
        }

        HashSet<string> indexableParkIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string parkId in parkEventCounts.Keys.Concat(parentParkItemEventCounts.Keys).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            int ownEventCount = parkEventCounts.GetValueOrDefault(parkId);
            int visibleEventCount = ownEventCount > 0
                ? ownEventCount
                : parentParkItemEventCounts.GetValueOrDefault(parkId);
            if (SeoPageValuePolicy.IsCollectionIndexable(visibleEventCount))
            {
                indexableParkIds.Add(parkId);
            }
        }

        return new HistoryTimelineIndexability(
            indexableParkIds,
            ResolveIndexableOwnerIds(parkItemEventCounts),
            ResolveIndexableOwnerIds(standaloneAttractionEventCounts));
    }

    public static string? ResolveParentParkId(HistorySitemapResolvedData resolvedData, HistoryEvent historyEvent)
    {
        if (!resolvedData.PublicItemById.TryGetValue(historyEvent.OwnerId, out ParkItem? item))
        {
            return null;
        }

        string? parkId = HistorySitemapCandidateResolver.NormalizeId(historyEvent.ContextParkId)
            ?? HistorySitemapCandidateResolver.NormalizeId(historyEvent.ParkId)
            ?? HistorySitemapCandidateResolver.NormalizeId(item.ParkId);

        return parkId is not null && resolvedData.PublicParkById.ContainsKey(parkId) ? parkId : null;
    }

    private static HashSet<string> ResolveIndexableOwnerIds(IReadOnlyDictionary<string, int> eventCounts)
    {
        return eventCounts
            .Where(static pair => SeoPageValuePolicy.IsCollectionIndexable(pair.Value))
            .Select(static pair => pair.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static void IncrementCount(Dictionary<string, int> counts, string id)
    {
        counts[id] = counts.GetValueOrDefault(id) + 1;
    }
}

internal sealed record HistoryTimelineIndexability(
    HashSet<string> ParkIds,
    HashSet<string> ParkItemIds,
    HashSet<string> StandaloneAttractionIds);
