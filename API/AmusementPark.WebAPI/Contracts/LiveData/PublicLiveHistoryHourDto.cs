namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed record PublicLiveHistoryHourDto(
    int LocalHour,
    string DataStatus,
    int ExpectedObservationCount,
    int ObservationCount,
    int UsableWaitCount,
    int DaysCovered,
    int ComparableDays,
    double CoveragePercent,
    double? RobustMinimumMinutes,
    double? FirstQuartileMinutes,
    double? MedianMinutes,
    double? ThirdQuartileMinutes,
    double? RobustMaximumMinutes);
