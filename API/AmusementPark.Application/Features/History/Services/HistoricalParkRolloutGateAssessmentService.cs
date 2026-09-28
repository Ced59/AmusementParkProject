using AmusementPark.Application.Features.History.Models;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

public sealed class HistoricalParkRolloutGateAssessmentService : IHistoricalParkRolloutGateAssessmentService
{
    private readonly IParkHistoricalSnapshotBuilder snapshotBuilder;
    private readonly HistoricalParkRolloutGateEvaluator gateEvaluator;

    public HistoricalParkRolloutGateAssessmentService(
        IParkHistoricalSnapshotBuilder snapshotBuilder,
        HistoricalParkRolloutGateEvaluator gateEvaluator)
    {
        this.snapshotBuilder = snapshotBuilder;
        this.gateEvaluator = gateEvaluator;
    }

    public HistoricalParkRolloutGate Assess(
        string parkId,
        IReadOnlyCollection<HistoricalSubject> subjects,
        IReadOnlyCollection<HistoricalFact> facts)
    {
        if (string.IsNullOrWhiteSpace(parkId))
        {
            throw new ArgumentException("A park identifier is required.", nameof(parkId));
        }

        ArgumentNullException.ThrowIfNull(subjects);
        ArgumentNullException.ThrowIfNull(facts);
        HistoricalFact[] publicFacts = facts
            .Where(static fact => fact.IsDecisionEligible
                && fact.Subject.PublicationPolicy != HistoricalSubjectPublicationPolicy.Suppressed)
            .ToArray();
        int[] candidateYears = publicFacts
            .Where(static fact => fact.Importance == HistoricalImportance.Major)
            .SelectMany(static fact => ResolveBoundaryYears(fact.Period))
            .Distinct()
            .Order()
            .ToArray();
        int[] indexableKeyYears = candidateYears
            .Where(year => HistoricalSnapshotSeoEligibilityEvaluator.IsIndexableKeyYear(
                this.snapshotBuilder.Build(
                    parkId.Trim(),
                    HistoricalInstant.ForYear(year),
                    subjects,
                    publicFacts),
                publicFacts))
            .ToArray();
        return this.gateEvaluator.Evaluate(publicFacts, indexableKeyYears);
    }

    public HistoricalParkRolloutGate AssessPublicPark(
        HistoricalParkEditorialScope scope,
        IReadOnlyCollection<HistoricalFact> facts)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(facts);
        if (!scope.IsPublicPark)
        {
            return this.Assess(scope.ParkId, Array.Empty<HistoricalSubject>(), Array.Empty<HistoricalFact>());
        }

        HashSet<HistoricalSubjectKey> publicCurrentSubjectKeys = scope.PublicCurrentSubjects
            .Select(static subject => new HistoricalSubjectKey(
                subject.Type,
                subject.Id,
                subject.ContextParkId))
            .ToHashSet();
        HistoricalFact[] publicFacts = facts
            .Where(fact => IsPublicFact(fact, scope.ParkId, publicCurrentSubjectKeys))
            .ToArray();
        HistoricalSubject[] publicSubjects = scope.PublicCurrentSubjects
            .Concat(publicFacts
                .Where(static fact => fact.Subject.PublicationPolicy
                    == HistoricalSubjectPublicationPolicy.HistoricalOnly)
                .Select(static fact => fact.Subject))
            .DistinctBy(static subject => new HistoricalSubjectKey(
                subject.Type,
                subject.Id,
                subject.ContextParkId))
            .ToArray();
        return this.Assess(scope.ParkId, publicSubjects, publicFacts);
    }

    private static bool IsPublicFact(
        HistoricalFact fact,
        string parkId,
        IReadOnlySet<HistoricalSubjectKey> publicCurrentSubjectKeys)
    {
        if (!fact.IsDecisionEligible
            || fact.Subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.Suppressed)
        {
            return false;
        }

        HistoricalSubjectKey key = new(
            fact.Subject.Type,
            fact.Subject.Id,
            fact.Subject.ContextParkId);
        return fact.Subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.HistoricalOnly
            ? string.Equals(fact.Subject.ContextParkId, parkId, StringComparison.Ordinal)
            : publicCurrentSubjectKeys.Contains(key);
    }

    private static IEnumerable<int> ResolveBoundaryYears(HistoricalPeriod period)
    {
        if (period.Start is not null)
        {
            yield return period.Start.Year;
        }

        if (period.End is not null && period.End.Year != period.Start?.Year)
        {
            yield return period.End.Year;
        }
    }
}
