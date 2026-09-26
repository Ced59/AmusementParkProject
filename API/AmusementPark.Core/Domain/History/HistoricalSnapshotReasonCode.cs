namespace AmusementPark.Core.Domain.History;

public enum HistoricalSnapshotReasonCode
{
    NoEligibleLifecycleFact = 0,
    BeforeConfirmedInitialOpening = 1,
    ConfirmedActivity = 2,
    ConfirmedClosure = 3,
    PartialLifecycleBoundary = 4,
    UncertainEvidence = 5,
    AmbiguousTransitionOrder = 6,
    InconsistentLifecycleSequence = 7,
    UnboundedTemporaryClosure = 8,
    UnclassifiedClosure = 9,
    PartialAttributeBoundary = 10,
    AmbiguousAttributeOrder = 11,
    InvalidStructuredAttributeValue = 12,
}
