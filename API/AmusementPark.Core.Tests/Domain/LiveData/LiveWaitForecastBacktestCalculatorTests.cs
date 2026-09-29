using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.LiveData;

public sealed class LiveWaitForecastBacktestCalculatorTests
{
    private static readonly DateTime StartsAtUtc =
        new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Calculate_ShouldApprovePilotOnlyWhenWeekdayCandidateBeatsBaseline()
    {
        LiveWaitForecastBacktestCalculator calculator = CreateCalculator();
        IReadOnlyCollection<LiveWaitHistoryObservation> observations = CreateDailyObservations(
            240,
            timestamp => 10 + (((int)timestamp.DayOfWeek) * 10));

        LiveWaitForecastBacktestReport report = calculator.Calculate(
            observations,
            StartsAtUtc.AddDays(120),
            StartsAtUtc.AddDays(240),
            CreateActiveWindow());

        Assert.Equal(LiveWaitForecastBacktestVerdict.EligibleForPilot, report.Verdict);
        Assert.Contains(LiveWaitForecastBacktestReason.CandidatePassed, report.Reasons);
        Assert.NotNull(report.Baseline);
        Assert.NotNull(report.Candidate);
        Assert.True(report.Baseline.MeanAbsoluteErrorMinutes > 0d);
        Assert.Equal(0d, report.Candidate.MeanAbsoluteErrorMinutes);
        Assert.True(report.MaeImprovementPercent >= report.Policy.RequiredMaeImprovementPercent);
        Assert.Equal(100d, report.IntervalCoveragePercent);
        Assert.False(report.DriftDetected);
        Assert.True(report.EvaluationPointCount >= report.Policy.MinimumEvaluationPoints);
    }

    [Fact]
    public void Calculate_ShouldRecommendAbandonmentWhenCandidateDoesNotBeatBaseline()
    {
        LiveWaitForecastBacktestCalculator calculator = CreateCalculator();
        IReadOnlyCollection<LiveWaitHistoryObservation> observations = CreateDailyObservations(
            240,
            _ => 20);

        LiveWaitForecastBacktestReport report = calculator.Calculate(
            observations,
            StartsAtUtc.AddDays(120),
            StartsAtUtc.AddDays(240),
            CreateActiveWindow());

        Assert.Equal(LiveWaitForecastBacktestVerdict.Abandon, report.Verdict);
        Assert.Contains(LiveWaitForecastBacktestReason.BaselineNotBeaten, report.Reasons);
        Assert.DoesNotContain(LiveWaitForecastBacktestReason.CandidatePassed, report.Reasons);
    }

    [Fact]
    public void Calculate_ShouldStayInconclusiveWhenEvaluationCoverageIsTooSmall()
    {
        LiveWaitForecastBacktestCalculator calculator = CreateCalculator();
        IReadOnlyCollection<LiveWaitHistoryObservation> observations = CreateDailyObservations(
            30,
            _ => 20);

        LiveWaitForecastBacktestReport report = calculator.Calculate(
            observations,
            StartsAtUtc.AddDays(14),
            StartsAtUtc.AddDays(30),
            CreateActiveWindow());

        Assert.Equal(LiveWaitForecastBacktestVerdict.InsufficientData, report.Verdict);
        Assert.Contains(
            LiveWaitForecastBacktestReason.InsufficientEvaluationPoints,
            report.Reasons);
        Assert.Null(report.Baseline);
        Assert.Null(report.Candidate);
    }

    [Fact]
    public void Calculate_ShouldIgnoreObservationsAfterEvaluationWindow()
    {
        LiveWaitForecastBacktestCalculator calculator = CreateCalculator();
        List<LiveWaitHistoryObservation> observations = CreateDailyObservations(
            240,
            timestamp => 10 + (((int)timestamp.DayOfWeek) * 10)).ToList();
        DateTime fromUtc = StartsAtUtc.AddDays(120);
        DateTime toUtc = StartsAtUtc.AddDays(240);
        LiveWaitForecastBacktestReport before = calculator.Calculate(
            observations,
            fromUtc,
            toUtc,
            CreateActiveWindow());
        observations.Add(CreateObservation(toUtc.AddDays(1), 1000));

        LiveWaitForecastBacktestReport after = calculator.Calculate(
            observations,
            fromUtc,
            toUtc,
            CreateActiveWindow());

        Assert.Equal(before.EvaluationPointCount, after.EvaluationPointCount);
        Assert.Equal(
            before.Candidate?.MeanAbsoluteErrorMinutes,
            after.Candidate?.MeanAbsoluteErrorMinutes);
        Assert.Equal(before.IntervalCoveragePercent, after.IntervalCoveragePercent);
    }

    [Fact]
    public void Calculate_ShouldBlockPilotWhenRecentErrorDrifts()
    {
        LiveWaitForecastBacktestCalculator calculator = CreateCalculator();
        IReadOnlyCollection<LiveWaitHistoryObservation> observations = CreateDailyObservations(
            240,
            timestamp =>
            {
                int weekdayPattern = 10 + (((int)timestamp.DayOfWeek) * 10);
                return timestamp >= StartsAtUtc.AddDays(180)
                    ? weekdayPattern + 100
                    : weekdayPattern;
            });

        LiveWaitForecastBacktestReport report = calculator.Calculate(
            observations,
            StartsAtUtc.AddDays(120),
            StartsAtUtc.AddDays(240),
            CreateActiveWindow());

        Assert.Equal(LiveWaitForecastBacktestVerdict.Abandon, report.Verdict);
        Assert.True(report.DriftDetected);
        Assert.Contains(LiveWaitForecastBacktestReason.DriftDetected, report.Reasons);
        Assert.True(report.RecentCandidateMaeMinutes > report.OlderCandidateMaeMinutes);
    }

    [Fact]
    public void Calculate_ShouldNotUseTrainingPointsBeforeTheyAreReceived()
    {
        LiveWaitForecastBacktestCalculator calculator = CreateCalculator();
        DateTime fromUtc = StartsAtUtc.AddDays(120);
        DateTime toUtc = StartsAtUtc.AddDays(240);
        IReadOnlyCollection<LiveWaitHistoryObservation> complete = CreateDailyObservations(
            240,
            timestamp => 10 + (((int)timestamp.DayOfWeek) * 10));
        LiveWaitHistoryObservation[] withoutDelayedPeriod = complete
            .Where(observation => observation.ObservedAtUtc < StartsAtUtc.AddDays(40)
                || observation.ObservedAtUtc > StartsAtUtc.AddDays(70))
            .ToArray();
        List<LiveWaitHistoryObservation> withDelayedPeriod = withoutDelayedPeriod.ToList();
        DateTime lateReceiptUtc = toUtc.AddMinutes(-1);
        withDelayedPeriod.AddRange(Enumerable.Range(40, 31)
            .Select(day => CreateObservation(
                StartsAtUtc.AddDays(day),
                500,
                lateReceiptUtc)));

        LiveWaitForecastBacktestReport withoutDelayed = calculator.Calculate(
            withoutDelayedPeriod,
            fromUtc,
            toUtc,
            CreateActiveWindow());
        LiveWaitForecastBacktestReport withDelayed = calculator.Calculate(
            withDelayedPeriod,
            fromUtc,
            toUtc,
            CreateActiveWindow());

        Assert.Equal(withoutDelayed.EvaluationPointCount, withDelayed.EvaluationPointCount);
        Assert.Equal(
            withoutDelayed.Baseline?.MeanAbsoluteErrorMinutes,
            withDelayed.Baseline?.MeanAbsoluteErrorMinutes);
        Assert.Equal(
            withoutDelayed.Candidate?.MeanAbsoluteErrorMinutes,
            withDelayed.Candidate?.MeanAbsoluteErrorMinutes);
        Assert.Equal(withoutDelayed.IntervalCoveragePercent, withDelayed.IntervalCoveragePercent);
        Assert.Equal(
            withoutDelayed.MedianIntervalWidthMinutes,
            withDelayed.MedianIntervalWidthMinutes);
    }

    [Fact]
    public void Policy_ShouldUseRawMetricsAtDecisionBoundaries()
    {
        LiveWaitForecastBacktestPolicy policy = new LiveWaitForecastBacktestPolicy();

        Assert.False(policy.HasRequiredMaeImprovement(10.04d, 9.54d));
        Assert.True(policy.HasRequiredMaeImprovement(10d, 9.5d));
        Assert.False(policy.IsIntervalUseful(69.96d, 59.96d));
        Assert.False(policy.IsIntervalUseful(70.04d, 60.04d));
        Assert.False(policy.IsDriftDetected(10d, 12.99d));
        Assert.True(policy.IsDriftDetected(10d, 13d));
    }

    private static LiveWaitForecastBacktestCalculator CreateCalculator()
    {
        return new LiveWaitForecastBacktestCalculator(new LiveWaitForecastBacktestPolicy());
    }

    private static LivePollingActiveWindow CreateActiveWindow()
    {
        return new LivePollingActiveWindow(TimeZoneInfo.Utc, 0, 24);
    }

    private static IReadOnlyCollection<LiveWaitHistoryObservation> CreateDailyObservations(
        int dayCount,
        Func<DateTime, int> waitFactory)
    {
        return Enumerable.Range(0, dayCount)
            .Select(day => StartsAtUtc.AddDays(day))
            .Select(timestamp => CreateObservation(timestamp, waitFactory(timestamp)))
            .ToArray();
    }

    private static LiveWaitHistoryObservation CreateObservation(
        DateTime observedAtUtc,
        int waitMinutes,
        DateTime? receivedAtUtc = null)
    {
        return new LiveWaitHistoryObservation(
            "external-item-1",
            "mapping-1",
            observedAtUtc,
            receivedAtUtc ?? observedAtUtc.AddSeconds(1),
            LiveOperationalStatus.Open,
            new[]
            {
                new LiveQueueObservation(LiveQueueKind.Standby, waitMinutes, false),
            },
            false);
    }
}
