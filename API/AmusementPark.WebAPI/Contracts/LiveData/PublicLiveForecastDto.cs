namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed record PublicLiveForecastDto(
    string TargetDisplayName,
    string ParkDisplayName,
    string TimeZoneId,
    DateTime ForecastFromUtc,
    DateTime ForecastToUtc,
    DateTime CalculatedAtUtc,
    double ExpectedWaitMinutes,
    double LowerBoundMinutes,
    double UpperBoundMinutes,
    int TrainingDayCount,
    string StudyVersion,
    string Method,
    string IntervalMethod,
    double MeanAbsoluteErrorMinutes,
    double IntervalCoveragePercent,
    DateTime EvaluationFromUtc,
    DateTime EvaluationToUtc,
    int EvaluationPointCount,
    PublicLiveSourceDto Source);
