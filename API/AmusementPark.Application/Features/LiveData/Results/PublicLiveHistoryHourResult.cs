using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Results;

public sealed record PublicLiveHistoryHourResult(
    int LocalHour,
    LiveWaitHistoryDataStatus DataStatus,
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
