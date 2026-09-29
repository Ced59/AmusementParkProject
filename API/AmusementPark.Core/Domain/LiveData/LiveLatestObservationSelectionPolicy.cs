namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveLatestObservationSelectionPolicy
{
    public LiveLatestObservation? Select(
        IReadOnlyCollection<LiveLatestObservation> observations,
        IReadOnlyDictionary<LiveDataSourceId, int> sourcePriorities,
        DateTime asOfUtc)
    {
        ArgumentNullException.ThrowIfNull(observations);
        ArgumentNullException.ThrowIfNull(sourcePriorities);
        if (asOfUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The selection instant must be expressed in UTC.", nameof(asOfUtc));
        }

        List<LiveLatestObservation> eligible = observations
            .Where(observation => sourcePriorities.ContainsKey(observation.Provenance.SourceId))
            .ToList();
        List<LiveLatestObservation> current = eligible
            .Where(observation => observation.FreshnessPolicy
                .Assess(observation.Provenance.ObservedAtUtc, asOfUtc)
                .CanBePresentedAsCurrent)
            .ToList();
        return SelectByPriorityThenRecency(
            current.Count > 0 ? current : eligible,
            sourcePriorities);
    }

    private static LiveLatestObservation? SelectByPriorityThenRecency(
        IReadOnlyCollection<LiveLatestObservation> observations,
        IReadOnlyDictionary<LiveDataSourceId, int> sourcePriorities)
    {
        return observations
            .OrderBy(observation => sourcePriorities[observation.Provenance.SourceId])
            .ThenByDescending(static observation => observation.Provenance.ObservedAtUtc)
            .ThenByDescending(static observation => observation.Provenance.ReceivedAtUtc)
            .ThenBy(static observation => observation.Provenance.SourceId.Value, StringComparer.Ordinal)
            .FirstOrDefault();
    }
}
