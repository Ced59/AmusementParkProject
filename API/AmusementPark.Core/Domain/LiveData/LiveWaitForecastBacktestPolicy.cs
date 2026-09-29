namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveWaitForecastBacktestPolicy
{
    public const string StudyVersion = "live-wait-backtest-v1";
    public const string BaselineMethod = "rolling-hourly-median-v1";
    public const string CandidateMethod = "rolling-weekday-hour-median-v1";
    public const string IntervalMethod = "rolling-weekday-hour-p10-p90-v1";

    public int TrainingWindowDays => 84;

    public int MinimumBaselineTrainingDays => 28;

    public int MinimumCandidateTrainingDays => 8;

    public int MinimumEvaluationDays => 14;

    public int MinimumEvaluationPoints => 100;

    public double RequiredMaeImprovementPercent => 5d;

    public double NominalIntervalCoveragePercent => 80d;

    public double MinimumIntervalCoveragePercent => 70d;

    public double MaximumUsefulMedianIntervalWidthMinutes => 60d;

    public double DriftThresholdPercent => 25d;

    public double MinimumDriftIncreaseMinutes => 3d;
}
