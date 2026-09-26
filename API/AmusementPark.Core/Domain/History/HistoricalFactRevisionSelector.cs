namespace AmusementPark.Core.Domain.History;

internal static class HistoricalFactRevisionSelector
{
    internal static IReadOnlyList<HistoricalFact> SelectDecisionEligible(
        IReadOnlyCollection<HistoricalFact> facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return facts
            .GroupBy(static fact => fact.Id)
            .Select(static revisions => revisions
                .OrderByDescending(static fact => fact.Revision)
                .ThenByDescending(static fact => fact.RecordedAtUtc)
                .First())
            .Where(static fact => fact.IsDecisionEligible)
            .OrderBy(static fact => fact.Subject.Type)
            .ThenBy(static fact => fact.Subject.Id, StringComparer.Ordinal)
            .ThenBy(static fact => fact.Id)
            .ToArray();
    }
}
