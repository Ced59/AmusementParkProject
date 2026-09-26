namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalAmbiguity
{
    public HistoricalAmbiguity(
        HistoricalSubject subject,
        HistoricalSnapshotReasonCode code,
        HistoricalAttributeKind? attributeKind,
        IReadOnlyCollection<Guid> factIds)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(factIds);
        if (!Enum.IsDefined(code)
            || !IsAmbiguityCode(code)
            || (attributeKind.HasValue && !Enum.IsDefined(attributeKind.Value)))
        {
            throw new ArgumentOutOfRangeException(nameof(code));
        }

        bool requiresAttribute = code is HistoricalSnapshotReasonCode.PartialAttributeBoundary
            or HistoricalSnapshotReasonCode.AmbiguousAttributeOrder
            or HistoricalSnapshotReasonCode.InvalidStructuredAttributeValue;
        bool requiresLifecycle = code is HistoricalSnapshotReasonCode.NoEligibleLifecycleFact
            or HistoricalSnapshotReasonCode.PartialLifecycleBoundary
            or HistoricalSnapshotReasonCode.AmbiguousTransitionOrder
            or HistoricalSnapshotReasonCode.InconsistentLifecycleSequence
            or HistoricalSnapshotReasonCode.UnboundedTemporaryClosure
            or HistoricalSnapshotReasonCode.UnclassifiedClosure;
        if ((requiresAttribute && !attributeKind.HasValue)
            || (requiresLifecycle && attributeKind.HasValue))
        {
            throw new ArgumentException(
                "The historical ambiguity scope is inconsistent with its reason.",
                nameof(attributeKind));
        }

        this.Subject = subject;
        this.Code = code;
        this.AttributeKind = attributeKind;
        this.FactIds = Array.AsReadOnly(
            factIds
                .Where(static factId => factId != Guid.Empty)
                .Distinct()
                .OrderBy(static factId => factId)
                .ToArray());
    }

    public HistoricalSubject Subject { get; }

    public HistoricalSnapshotReasonCode Code { get; }

    public HistoricalAttributeKind? AttributeKind { get; }

    public IReadOnlyList<Guid> FactIds { get; }

    internal static bool IsAmbiguityCode(HistoricalSnapshotReasonCode code)
    {
        return code is HistoricalSnapshotReasonCode.NoEligibleLifecycleFact
            or HistoricalSnapshotReasonCode.PartialLifecycleBoundary
            or HistoricalSnapshotReasonCode.UncertainEvidence
            or HistoricalSnapshotReasonCode.AmbiguousTransitionOrder
            or HistoricalSnapshotReasonCode.InconsistentLifecycleSequence
            or HistoricalSnapshotReasonCode.UnboundedTemporaryClosure
            or HistoricalSnapshotReasonCode.UnclassifiedClosure
            or HistoricalSnapshotReasonCode.PartialAttributeBoundary
            or HistoricalSnapshotReasonCode.AmbiguousAttributeOrder
            or HistoricalSnapshotReasonCode.InvalidStructuredAttributeValue;
    }
}
