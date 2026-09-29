using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Models;

public sealed record PublicLiveForecastComputation(
    string TimeZoneId,
    LiveWaitForecast Forecast,
    string StudyVersion,
    string Method,
    string IntervalMethod,
    double MeanAbsoluteErrorMinutes,
    double IntervalCoveragePercent,
    DateTime EvaluationFromUtc,
    DateTime EvaluationToUtc,
    int EvaluationPointCount);
