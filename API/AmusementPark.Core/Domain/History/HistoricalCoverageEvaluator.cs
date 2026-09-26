namespace AmusementPark.Core.Domain.History;

internal sealed class HistoricalCoverageEvaluator
{
    private const decimal MinimumSubstantialReliablePeriodPercentage = 50m;
    private const decimal MinimumSubstantialDatedPeriodPercentage = 75m;
    private const decimal MinimumSubstantialFieldPercentage = 50m;

    internal HistoricalCoverage Evaluate(
        IReadOnlyCollection<HistoricalSubjectSnapshot> subjects,
        IReadOnlyCollection<HistoricalFact> eligibleFacts,
        IReadOnlyCollection<HistoricalAmbiguity> ambiguities)
    {
        ArgumentNullException.ThrowIfNull(subjects);
        ArgumentNullException.ThrowIfNull(eligibleFacts);
        ArgumentNullException.ThrowIfNull(ambiguities);
        int undatedSubjectCount = subjects.Count(HasNoEligibleLifecycleFact);
        int reliablePeriodSubjectCount = subjects.Count(IsReliablePeriod);
        int partialPeriodSubjectCount = subjects.Count
            - reliablePeriodSubjectCount
            - undatedSubjectCount;
        HistoricalFieldCoverage nameCoverage = BuildFieldCoverage(
            subjects,
            HistoricalAttributeKind.Name,
            static _ => true);
        HistoricalFieldCoverage zoneCoverage = BuildFieldCoverage(
            subjects,
            HistoricalAttributeKind.Zone,
            static subject => subject.Subject.Type == HistoricalSubjectType.ParkItem);
        HashSet<(HistoricalSubjectType Type, string Id)> subjectKeys = subjects
            .Select(static subject => (subject.Subject.Type, subject.Subject.Id))
            .ToHashSet();
        DateTime? lastReviewedAtUtc = eligibleFacts
            .Where(fact => subjectKeys.Contains((fact.Subject.Type, fact.Subject.Id)))
            .Where(static fact => fact.VerifiedAtUtc.HasValue)
            .Select(static fact => fact.VerifiedAtUtc)
            .Max();
        HistoricalCoverageStatus status = ResolveStatus(
            subjects.Count,
            reliablePeriodSubjectCount,
            partialPeriodSubjectCount,
            undatedSubjectCount,
            nameCoverage,
            zoneCoverage,
            ambiguities.Count);

        return new HistoricalCoverage(
            subjects.Count,
            reliablePeriodSubjectCount,
            partialPeriodSubjectCount,
            undatedSubjectCount,
            nameCoverage,
            zoneCoverage,
            lastReviewedAtUtc,
            status);
    }

    private static bool HasNoEligibleLifecycleFact(HistoricalSubjectSnapshot subject)
    {
        return subject.Reasons.Any(static reason =>
            reason.Code == HistoricalSnapshotReasonCode.NoEligibleLifecycleFact);
    }

    private static bool IsReliablePeriod(HistoricalSubjectSnapshot subject)
    {
        if (HasNoEligibleLifecycleFact(subject)
            || subject.OperationalState is not (HistoricalOperationalState.KnownOpen
                or HistoricalOperationalState.KnownClosed))
        {
            return false;
        }

        return !subject.Reasons.Any(reason => reason.Code is (
                HistoricalSnapshotReasonCode.PartialLifecycleBoundary
                or HistoricalSnapshotReasonCode.UncertainEvidence
                or HistoricalSnapshotReasonCode.AmbiguousTransitionOrder
                or HistoricalSnapshotReasonCode.InconsistentLifecycleSequence
                or HistoricalSnapshotReasonCode.UnboundedTemporaryClosure
                or HistoricalSnapshotReasonCode.UnclassifiedClosure)
            && HasUnattributedLifecycleCause(subject, reason));
    }

    private static HistoricalFieldCoverage BuildFieldCoverage(
        IReadOnlyCollection<HistoricalSubjectSnapshot> subjects,
        HistoricalAttributeKind kind,
        Func<HistoricalSubjectSnapshot, bool> isApplicable)
    {
        HistoricalSubjectSnapshot[] applicableSubjects = subjects
            .Where(isApplicable)
            .ToArray();
        int documentedSubjectCount = applicableSubjects.Count(subject => subject.Attributes.Any(
            attribute => attribute.Kind == kind
                && attribute.State == HistoricalAttributeValueState.Known));
        return new HistoricalFieldCoverage(
            documentedSubjectCount,
            applicableSubjects.Length);
    }

    private static HistoricalCoverageStatus ResolveStatus(
        int totalSubjectCount,
        int reliablePeriodSubjectCount,
        int partialPeriodSubjectCount,
        int undatedSubjectCount,
        HistoricalFieldCoverage nameCoverage,
        HistoricalFieldCoverage zoneCoverage,
        int ambiguityCount)
    {
        if (totalSubjectCount > 0
            && reliablePeriodSubjectCount == totalSubjectCount
            && nameCoverage.IsComplete
            && zoneCoverage.IsComplete
            && ambiguityCount == 0)
        {
            return HistoricalCoverageStatus.HighConfidence;
        }

        if (totalSubjectCount == 0)
        {
            return HistoricalCoverageStatus.Partial;
        }

        decimal reliablePeriodPercentage = Percentage(reliablePeriodSubjectCount, totalSubjectCount);
        decimal datedPeriodPercentage = Percentage(
            reliablePeriodSubjectCount + partialPeriodSubjectCount,
            totalSubjectCount);
        bool fieldsAreSubstantial = nameCoverage.Percentage >= MinimumSubstantialFieldPercentage
            && zoneCoverage.Percentage >= MinimumSubstantialFieldPercentage;
        return reliablePeriodPercentage >= MinimumSubstantialReliablePeriodPercentage
            && datedPeriodPercentage >= MinimumSubstantialDatedPeriodPercentage
            && fieldsAreSubstantial
            && undatedSubjectCount < totalSubjectCount
                ? HistoricalCoverageStatus.Substantial
                : HistoricalCoverageStatus.Partial;
    }

    private static decimal Percentage(int numerator, int denominator)
    {
        return 100m * numerator / denominator;
    }

    private static bool HasUnattributedLifecycleCause(
        HistoricalSubjectSnapshot subject,
        HistoricalSnapshotReason reason)
    {
        HistoricalSnapshotReason[] attributedReasons = subject.Attributes
            .SelectMany(static attribute => attribute.Reasons)
            .Where(attributeReason => attributeReason.Code == reason.Code)
            .ToArray();
        if (reason.FactIds.Count == 0)
        {
            return attributedReasons.Length == 0;
        }

        HashSet<Guid> attributedFactIds = attributedReasons
            .SelectMany(static attributeReason => attributeReason.FactIds)
            .ToHashSet();
        return reason.FactIds.Any(factId => !attributedFactIds.Contains(factId));
    }

}
