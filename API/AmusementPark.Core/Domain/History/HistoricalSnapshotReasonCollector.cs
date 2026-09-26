namespace AmusementPark.Core.Domain.History;

internal sealed class HistoricalSnapshotReasonCollector
{
    private readonly Dictionary<HistoricalSnapshotReasonCode, HashSet<Guid>> factIdsByCode = new();

    internal void Add(HistoricalSnapshotReasonCode code, HistoricalFact fact)
    {
        this.Add(code, new[] { fact.Id });
    }

    internal void Add(HistoricalSnapshotReasonCode code, IEnumerable<HistoricalFact> facts)
    {
        this.Add(code, facts.Select(static fact => fact.Id));
    }

    internal void Add(HistoricalSnapshotReasonCode code, IEnumerable<Guid> factIds)
    {
        if (!this.factIdsByCode.TryGetValue(code, out HashSet<Guid>? collectedFactIds))
        {
            collectedFactIds = new HashSet<Guid>();
            this.factIdsByCode.Add(code, collectedFactIds);
        }

        collectedFactIds.UnionWith(factIds.Where(static factId => factId != Guid.Empty));
    }

    internal IReadOnlyList<HistoricalSnapshotReason> Build()
    {
        return this.factIdsByCode
            .OrderBy(static pair => pair.Key)
            .Select(static pair => new HistoricalSnapshotReason(pair.Key, pair.Value))
            .ToArray();
    }
}
