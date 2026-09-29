using AmusementPark.Application.Features.LiveData.Results;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.WebAPI.Contracts.LiveData;

namespace AmusementPark.WebAPI.Mappers;

public static class LiveWaitForecastBacktestHttpMapper
{
    public static LiveWaitForecastBacktestDto ToHttp(
        this LiveWaitForecastBacktestResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        LiveWaitForecastBacktestReport report = result.Report;
        return new LiveWaitForecastBacktestDto
        {
            TargetDisplayName = result.TargetDisplayName,
            ParkDisplayName = result.ParkDisplayName,
            StudyVersion = report.StudyVersion,
            Verdict = report.Verdict.ToString(),
            Reasons = report.Reasons.Select(static reason => reason.ToString()).ToArray(),
            EvaluationFromUtc = report.EvaluationFromUtc,
            EvaluationToUtc = report.EvaluationToUtc,
            TimeZoneId = report.TimeZoneId,
            SourceObservationCount = report.SourceObservationCount,
            HourlyPointCount = report.HourlyPointCount,
            EvaluationPointCount = report.EvaluationPointCount,
            EvaluationDays = report.EvaluationDays,
            Baseline = report.Baseline?.ToHttp(),
            Candidate = report.Candidate?.ToHttp(),
            MaeImprovementPercent = report.MaeImprovementPercent,
            BaselineMethod = LiveWaitForecastBacktestPolicy.BaselineMethod,
            CandidateMethod = LiveWaitForecastBacktestPolicy.CandidateMethod,
            IntervalMethod = LiveWaitForecastBacktestPolicy.IntervalMethod,
            IntervalCoveragePercent = report.IntervalCoveragePercent,
            MedianIntervalWidthMinutes = report.MedianIntervalWidthMinutes,
            OlderCandidateMaeMinutes = report.OlderCandidateMaeMinutes,
            RecentCandidateMaeMinutes = report.RecentCandidateMaeMinutes,
            DriftPercent = report.DriftPercent,
            DriftDetected = report.DriftDetected,
            Policy = new LiveWaitForecastBacktestPolicyDto
            {
                TrainingWindowDays = report.Policy.TrainingWindowDays,
                MinimumBaselineTrainingDays = report.Policy.MinimumBaselineTrainingDays,
                MinimumCandidateTrainingDays = report.Policy.MinimumCandidateTrainingDays,
                MinimumEvaluationDays = report.Policy.MinimumEvaluationDays,
                MinimumEvaluationPoints = report.Policy.MinimumEvaluationPoints,
                RequiredMaeImprovementPercent = report.Policy.RequiredMaeImprovementPercent,
                NominalIntervalCoveragePercent = report.Policy.NominalIntervalCoveragePercent,
                MinimumIntervalCoveragePercent = report.Policy.MinimumIntervalCoveragePercent,
                MaximumUsefulMedianIntervalWidthMinutes =
                    report.Policy.MaximumUsefulMedianIntervalWidthMinutes,
                DriftThresholdPercent = report.Policy.DriftThresholdPercent,
                MinimumDriftIncreaseMinutes = report.Policy.MinimumDriftIncreaseMinutes,
            },
            GeneratedAtUtc = result.GeneratedAtUtc,
        };
    }

    private static LiveWaitForecastBacktestMetricDto ToHttp(
        this LiveWaitForecastBacktestMetric metric)
    {
        return new LiveWaitForecastBacktestMetricDto
        {
            Method = metric.Method,
            MeanAbsoluteErrorMinutes = metric.MeanAbsoluteErrorMinutes,
            MedianAbsoluteErrorMinutes = metric.MedianAbsoluteErrorMinutes,
            P90AbsoluteErrorMinutes = metric.P90AbsoluteErrorMinutes,
        };
    }
}
