using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Results;

public sealed record PublicLiveHistoryResult(
    string TargetId,
    string DisplayName,
    string ParkId,
    string ParkDisplayName,
    DateTime FromUtc,
    DateTime ToUtc,
    string TimeZoneId,
    LiveWaitHistoryDataStatus DataStatus,
    int ExpectedObservationCount,
    int ObservationCount,
    int UsableWaitCount,
    int DaysCovered,
    int ComparableDays,
    double CoveragePercent,
    int TruncatedObservationCount,
    PublicLiveHistoryExclusionsResult Exclusions,
    IReadOnlyCollection<PublicLiveHistoryHourResult> Hours,
    PublicLiveSourceResult Source);
