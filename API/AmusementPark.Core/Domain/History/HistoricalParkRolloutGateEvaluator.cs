namespace AmusementPark.Core.Domain.History;

public sealed class HistoricalParkRolloutGateEvaluator
{
    public HistoricalParkRolloutGate Evaluate(
        IReadOnlyCollection<HistoricalFact> facts,
        IReadOnlyCollection<int> indexableKeyYears)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(indexableKeyYears);

        HistoricalFact[] publishedFacts = facts
            .Where(static fact => fact.IsDecisionEligible
                && fact.Subject.PublicationPolicy != HistoricalSubjectPublicationPolicy.Suppressed)
            .DistinctBy(static fact => fact.Id)
            .ToArray();
        return new HistoricalParkRolloutGate(
            publishedFacts.Length,
            publishedFacts.Count(static fact => fact.SourceReferences.Count > 0),
            publishedFacts.Count(static fact => fact.Importance == HistoricalImportance.Major),
            indexableKeyYears);
    }
}
