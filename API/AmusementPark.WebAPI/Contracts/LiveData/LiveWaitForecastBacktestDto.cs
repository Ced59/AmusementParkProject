namespace AmusementPark.WebAPI.Contracts.LiveData;

public sealed class LiveWaitForecastBacktestDto
{
    public string TargetDisplayName { get; set; } = string.Empty;

    public string ParkDisplayName { get; set; } = string.Empty;

    public string StudyVersion { get; set; } = string.Empty;

    public string Verdict { get; set; } = string.Empty;

    public IReadOnlyCollection<string> Reasons { get; set; } = Array.Empty<string>();

    public DateTime EvaluationFromUtc { get; set; }

    public DateTime EvaluationToUtc { get; set; }

    public string TimeZoneId { get; set; } = string.Empty;

    public int SourceObservationCount { get; set; }

    public int HourlyPointCount { get; set; }

    public int EvaluationPointCount { get; set; }

    public int EvaluationDays { get; set; }

    public LiveWaitForecastBacktestMetricDto? Baseline { get; set; }

    public LiveWaitForecastBacktestMetricDto? Candidate { get; set; }

    public double? MaeImprovementPercent { get; set; }

    public string IntervalMethod { get; set; } = string.Empty;

    public double? IntervalCoveragePercent { get; set; }

    public double? MedianIntervalWidthMinutes { get; set; }

    public double? OlderCandidateMaeMinutes { get; set; }

    public double? RecentCandidateMaeMinutes { get; set; }

    public double? DriftPercent { get; set; }

    public bool DriftDetected { get; set; }

    public LiveWaitForecastBacktestPolicyDto Policy { get; set; } =
        new LiveWaitForecastBacktestPolicyDto();

    public DateTime GeneratedAtUtc { get; set; }
}
