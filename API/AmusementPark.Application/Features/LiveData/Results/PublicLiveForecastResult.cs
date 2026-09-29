using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Results;

public sealed record PublicLiveForecastResult(
    string TargetDisplayName,
    string ParkDisplayName,
    string TimeZoneId,
    LiveWaitForecast Forecast,
    string StudyVersion,
    string Method,
    string IntervalMethod,
    double MeanAbsoluteErrorMinutes,
    double IntervalCoveragePercent,
    DateTime EvaluationFromUtc,
    DateTime EvaluationToUtc,
    int EvaluationPointCount,
    PublicLiveSourceResult Source);
