namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalSubjectSnapshot
{
    public HistoricalSubjectSnapshot(
        HistoricalSubject subject,
        HistoricalOperationalState operationalState,
        HistoricalPresenceExtent presenceExtent,
        IReadOnlyCollection<HistoricalPresenceInterval> confirmedPresenceIntervals,
        IReadOnlyCollection<HistoricalAttributeSnapshot> attributes,
        IReadOnlyCollection<HistoricalSnapshotReason> reasons,
        IReadOnlyCollection<Guid> supportingFactIds)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(confirmedPresenceIntervals);
        ArgumentNullException.ThrowIfNull(attributes);
        ArgumentNullException.ThrowIfNull(reasons);
        ArgumentNullException.ThrowIfNull(supportingFactIds);
        if (!Enum.IsDefined(operationalState) || !Enum.IsDefined(presenceExtent))
        {
            throw new ArgumentOutOfRangeException(nameof(operationalState));
        }

        bool presenceIsConsistent = operationalState == HistoricalOperationalState.KnownOpen
            ? presenceExtent != HistoricalPresenceExtent.None && confirmedPresenceIntervals.Count > 0
            : presenceExtent == HistoricalPresenceExtent.None && confirmedPresenceIntervals.Count == 0;
        if (!presenceIsConsistent)
        {
            throw new ArgumentException("The historical subject presence result is inconsistent.");
        }

        HistoricalAttributeKind[] attributeKinds = attributes
            .Select(static attribute => attribute.Kind)
            .ToArray();
        if (attributeKinds.Distinct().Count() != attributeKinds.Length)
        {
            throw new ArgumentException("A historical attribute can occur only once in a subject snapshot.");
        }

        this.Subject = subject;
        this.OperationalState = operationalState;
        this.PresenceExtent = presenceExtent;
        this.ConfirmedPresenceIntervals = Array.AsReadOnly(
            confirmedPresenceIntervals.OrderBy(static interval => interval.Start).ToArray());
        this.Attributes = Array.AsReadOnly(
            attributes.OrderBy(static attribute => attribute.Kind).ToArray());
        this.Reasons = Array.AsReadOnly(
            reasons.OrderBy(static reason => reason.Code).ToArray());
        this.SupportingFactIds = Array.AsReadOnly(
            supportingFactIds
                .Where(static factId => factId != Guid.Empty)
                .Distinct()
                .OrderBy(static factId => factId)
                .ToArray());
    }

    public HistoricalSubject Subject { get; }

    public HistoricalOperationalState OperationalState { get; }

    public HistoricalPresenceExtent PresenceExtent { get; }

    public IReadOnlyList<HistoricalPresenceInterval> ConfirmedPresenceIntervals { get; }

    public IReadOnlyList<HistoricalAttributeSnapshot> Attributes { get; }

    public IReadOnlyList<HistoricalSnapshotReason> Reasons { get; }

    public IReadOnlyList<Guid> SupportingFactIds { get; }
}
