namespace AmusementPark.Core.Domain.LiveData;

public sealed record LiveWaitForecast(
    DateTime ForecastFromUtc,
    DateTime ForecastToUtc,
    DateTime CalculatedAtUtc,
    double ExpectedWaitMinutes,
    double LowerBoundMinutes,
    double UpperBoundMinutes,
    int TrainingDayCount);
