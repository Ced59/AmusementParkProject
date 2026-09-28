namespace AmusementPark.Core.Domain.LiveData;

public enum LiveFreshnessReason
{
    WithinFreshWindow,
    WithinAgingWindow,
    WithinStaleWindow,
    ObservationExpired,
    MissingSourceTimestamp,
    SourceTimestampTooFarInFuture,
}
