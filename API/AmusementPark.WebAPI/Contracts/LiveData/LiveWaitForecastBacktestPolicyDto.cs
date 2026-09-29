namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed class LiveWaitForecastBacktestPolicyDto
{
    public int TrainingWindowDays { get; set; }

    public int MinimumBaselineTrainingDays { get; set; }

    public int MinimumCandidateTrainingDays { get; set; }

    public int MinimumEvaluationDays { get; set; }

    public int MinimumEvaluationPoints { get; set; }

    public double RequiredMaeImprovementPercent { get; set; }

    public double NominalIntervalCoveragePercent { get; set; }

    public double MinimumIntervalCoveragePercent { get; set; }

    public double MaximumUsefulMedianIntervalWidthMinutes { get; set; }

    public double DriftThresholdPercent { get; set; }

    public double MinimumDriftIncreaseMinutes { get; set; }
}
