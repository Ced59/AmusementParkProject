namespace AmusementPark.Core.Domain.LiveData;

public enum LiveWaitForecastBacktestReason
{
    InsufficientEvaluationPoints = 1,
    InsufficientEvaluationDays = 2,
    BaselineNotBeaten = 3,
    IntervalMiscalibrated = 4,
    DriftDetected = 5,
    CandidatePassed = 6,
}
