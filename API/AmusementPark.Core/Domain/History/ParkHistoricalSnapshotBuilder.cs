namespace AmusementPark.Core.Domain.History;

public sealed class ParkHistoricalSnapshotBuilder : IParkHistoricalSnapshotBuilder
{
    public const string CurrentMethodologyVersion = "hist-snapshot-v1";

    private readonly HistoricalLifecycleSnapshotReducer lifecycleReducer =
        new HistoricalLifecycleSnapshotReducer();
    private readonly HistoricalAttributeSnapshotReducer attributeReducer =
        new HistoricalAttributeSnapshotReducer();

    public ParkHistoricalSnapshot Build(
        string parkId,
        HistoricalInstant requestedInstant,
        IReadOnlyCollection<HistoricalSubject> subjects,
        IReadOnlyCollection<HistoricalFact> facts)
    {
        ArgumentNullException.ThrowIfNull(requestedInstant);
        ArgumentNullException.ThrowIfNull(subjects);
        ArgumentNullException.ThrowIfNull(facts);
        EnsureSubjectsAreUnique(subjects);
        IReadOnlyList<DateOnly> requestedDates = EnumerateDates(requestedInstant);
        IReadOnlyList<HistoricalFact> eligibleFacts =
            HistoricalFactRevisionSelector.SelectDecisionEligible(facts);
        List<HistoricalSubjectSnapshot> subjectSnapshots = new List<HistoricalSubjectSnapshot>();
        foreach (HistoricalSubject subject in subjects)
        {
            HistoricalFact[] subjectFacts = eligibleFacts
                .Where(fact => fact.Subject.Type == subject.Type
                    && string.Equals(fact.Subject.Id, subject.Id, StringComparison.Ordinal))
                .ToArray();
            HistoricalLifecycleReduction lifecycle = this.lifecycleReducer.Reduce(
                subjectFacts,
                requestedDates);
            IReadOnlyList<HistoricalAttributeSnapshot> attributes = this.attributeReducer.Reduce(
                subjectFacts,
                requestedDates);
            HistoricalSnapshotReasonCollector reasons = new HistoricalSnapshotReasonCollector();
            foreach (HistoricalSnapshotReason reason in lifecycle.Reasons
                         .Concat(attributes.SelectMany(static attribute => attribute.Reasons)))
            {
                reasons.Add(reason.Code, reason.FactIds);
            }

            subjectSnapshots.Add(new HistoricalSubjectSnapshot(
                subject,
                lifecycle.State,
                lifecycle.PresenceExtent,
                lifecycle.ConfirmedPresenceIntervals,
                attributes,
                reasons.Build(),
                subjectFacts.Select(static fact => fact.Id).ToArray()));
        }

        return new ParkHistoricalSnapshot(
            parkId,
            requestedInstant,
            subjectSnapshots,
            CurrentMethodologyVersion);
    }

    private static IReadOnlyList<DateOnly> EnumerateDates(HistoricalInstant instant)
    {
        HistoricalDateEnvelope envelope = instant.GetEnvelope();
        DateOnly start = envelope.EarliestPossibleDate
            ?? throw new InvalidOperationException("A requested historical instant requires a lower boundary.");
        DateOnly end = envelope.LatestPossibleDate
            ?? throw new InvalidOperationException("A requested historical instant requires an upper boundary.");
        List<DateOnly> dates = new List<DateOnly>();
        DateOnly current = start;
        while (current <= end)
        {
            dates.Add(current);
            if (current == end)
            {
                break;
            }

            current = current.AddDays(1);
        }

        return dates;
    }

    private static void EnsureSubjectsAreUnique(IReadOnlyCollection<HistoricalSubject> subjects)
    {
        int distinctCount = subjects
            .Select(static subject => (subject.Type, subject.Id))
            .Distinct()
            .Count();
        if (distinctCount != subjects.Count)
        {
            throw new ArgumentException("A historical snapshot subject must be unique.", nameof(subjects));
        }
    }
}
