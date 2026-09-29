namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveWaitForecastBacktestReport
{
    public LiveWaitForecastBacktestReport(
        DateTime evaluationFromUtc,
        DateTime evaluationToUtc,
        string timeZoneId,
        LiveWaitForecastBacktestVerdict verdict,
        IReadOnlyCollection<LiveWaitForecastBacktestReason> reasons,
        int sourceObservationCount,
        int hourlyPointCount,
        int evaluationPointCount,
        int evaluationDays,
        LiveWaitForecastBacktestMetric? baseline,
        LiveWaitForecastBacktestMetric? candidate,
        double? maeImprovementPercent,
        double? intervalCoveragePercent,
        double? medianIntervalWidthMinutes,
        double? olderCandidateMaeMinutes,
        double? recentCandidateMaeMinutes,
        double? driftPercent,
        bool driftDetected,
        LiveWaitForecastBacktestPolicy policy)
    {
        this.EvaluationFromUtc = evaluationFromUtc;
        this.EvaluationToUtc = evaluationToUtc;
        this.TimeZoneId = timeZoneId;
        this.Verdict = verdict;
        this.Reasons = reasons.ToArray();
        this.SourceObservationCount = sourceObservationCount;
        this.HourlyPointCount = hourlyPointCount;
        this.EvaluationPointCount = evaluationPointCount;
        this.EvaluationDays = evaluationDays;
        this.Baseline = baseline;
        this.Candidate = candidate;
        this.MaeImprovementPercent = maeImprovementPercent;
        this.IntervalCoveragePercent = intervalCoveragePercent;
        this.MedianIntervalWidthMinutes = medianIntervalWidthMinutes;
        this.OlderCandidateMaeMinutes = olderCandidateMaeMinutes;
        this.RecentCandidateMaeMinutes = recentCandidateMaeMinutes;
        this.DriftPercent = driftPercent;
        this.DriftDetected = driftDetected;
        this.Policy = policy;
    }

    public string StudyVersion => LiveWaitForecastBacktestPolicy.StudyVersion;

    public DateTime EvaluationFromUtc { get; }

    public DateTime EvaluationToUtc { get; }

    public string TimeZoneId { get; }

    public LiveWaitForecastBacktestVerdict Verdict { get; }

    public IReadOnlyCollection<LiveWaitForecastBacktestReason> Reasons { get; }

    public int SourceObservationCount { get; }

    public int HourlyPointCount { get; }

    public int EvaluationPointCount { get; }

    public int EvaluationDays { get; }

    public LiveWaitForecastBacktestMetric? Baseline { get; }

    public LiveWaitForecastBacktestMetric? Candidate { get; }

    public double? MaeImprovementPercent { get; }

    public double? IntervalCoveragePercent { get; }

    public double? MedianIntervalWidthMinutes { get; }

    public double? OlderCandidateMaeMinutes { get; }

    public double? RecentCandidateMaeMinutes { get; }

    public double? DriftPercent { get; }

    public bool DriftDetected { get; }

    public LiveWaitForecastBacktestPolicy Policy { get; }
}
