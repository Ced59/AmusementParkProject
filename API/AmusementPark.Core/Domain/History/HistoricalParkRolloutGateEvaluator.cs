namespace AmusementPark.Core.Domain.History;

public sealed class HistoricalParkRolloutGateEvaluator
{
    public HistoricalParkRolloutGate Evaluate(
        IReadOnlyCollection<HistoricalFact> facts,
        IReadOnlyCollection<int> indexableKeyYears,
        IReadOnlySet<(Guid SourceId, int Revision)> currentlyAdmissibleSourceRevisions)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(indexableKeyYears);
        ArgumentNullException.ThrowIfNull(currentlyAdmissibleSourceRevisions);

        HistoricalFact[] publishedFacts = facts
            .Where(static fact => fact.IsDecisionEligible
                && fact.Subject.PublicationPolicy != HistoricalSubjectPublicationPolicy.Suppressed)
            .DistinctBy(static fact => fact.Id)
            .ToArray();
        return new HistoricalParkRolloutGate(
            publishedFacts.Length,
            publishedFacts.Count(fact =>
                HistoricalFactEvidenceValidator.HasCurrentlyAdmissiblePublicEvidence(
                    fact,
                    currentlyAdmissibleSourceRevisions)),
            publishedFacts.Count(static fact => fact.Importance == HistoricalImportance.Major),
            indexableKeyYears);
    }
}
