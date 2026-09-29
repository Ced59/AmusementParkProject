namespace AmusementPark.Core.Domain.LiveData;

public enum LiveQualityIncidentReason
{
    UnmappedTarget = 0,
    IneligibleMapping = 1,
    InvalidFreshness = 2,
    StatusQueueConflict = 3,
    ProviderDiagnostic = 4,
}
