using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Core.Domain.Watchlists;

public sealed record LiveAlertTrigger(
    LiveAlertType Type,
    LiveOperationalStatus? PreviousStatus,
    LiveOperationalStatus CurrentStatus,
    int? PreviousWaitMinutes,
    int? CurrentWaitMinutes,
    int? ThresholdMinutes,
    LiveDataSourceId SourceId,
    DateTime ObservedAtUtc,
    DateTime TriggeredAtUtc,
    long AgeSeconds);
