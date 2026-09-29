namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed record PublicLiveHistoryDto(
    string TargetId,
    string DisplayName,
    string ParkId,
    string ParkDisplayName,
    DateTime FromUtc,
    DateTime ToUtc,
    string TimeZoneId,
    string DataStatus,
    int ExpectedObservationCount,
    int ObservationCount,
    int UsableWaitCount,
    int DaysCovered,
    int ComparableDays,
    double CoveragePercent,
    int TruncatedObservationCount,
    PublicLiveHistoryExclusionsDto Exclusions,
    IReadOnlyCollection<PublicLiveHistoryHourDto> Hours,
    PublicLiveSourceDto Source);
