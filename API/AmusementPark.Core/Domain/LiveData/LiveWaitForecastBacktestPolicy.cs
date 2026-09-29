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

    public double CalculateMaeImprovementPercent(
        double baselineMeanAbsoluteErrorMinutes,
        double candidateMeanAbsoluteErrorMinutes)
    {
        return baselineMeanAbsoluteErrorMinutes <= 0d
            ? candidateMeanAbsoluteErrorMinutes <= 0d ? 0d : -100d
            : (baselineMeanAbsoluteErrorMinutes - candidateMeanAbsoluteErrorMinutes)
                * 100d
                / baselineMeanAbsoluteErrorMinutes;
    }

    public bool HasRequiredMaeImprovement(
        double baselineMeanAbsoluteErrorMinutes,
        double candidateMeanAbsoluteErrorMinutes)
    {
        return this.CalculateMaeImprovementPercent(
            baselineMeanAbsoluteErrorMinutes,
            candidateMeanAbsoluteErrorMinutes) >= this.RequiredMaeImprovementPercent;
    }

    public bool IsIntervalUseful(
        double intervalCoveragePercent,
        double medianIntervalWidthMinutes)
    {
        return intervalCoveragePercent >= this.MinimumIntervalCoveragePercent
            && medianIntervalWidthMinutes <= this.MaximumUsefulMedianIntervalWidthMinutes;
    }

    public bool IsDriftDetected(
        double olderMeanAbsoluteErrorMinutes,
        double recentMeanAbsoluteErrorMinutes)
    {
        double driftPercent = this.CalculateDriftPercent(
            olderMeanAbsoluteErrorMinutes,
            recentMeanAbsoluteErrorMinutes);
        return recentMeanAbsoluteErrorMinutes - olderMeanAbsoluteErrorMinutes
                >= this.MinimumDriftIncreaseMinutes
            && driftPercent >= this.DriftThresholdPercent;
    }

    public double CalculateDriftPercent(
        double olderMeanAbsoluteErrorMinutes,
        double recentMeanAbsoluteErrorMinutes)
    {
        return (recentMeanAbsoluteErrorMinutes - olderMeanAbsoluteErrorMinutes)
            * 100d
            / Math.Max(olderMeanAbsoluteErrorMinutes, 1d);
    }
}
