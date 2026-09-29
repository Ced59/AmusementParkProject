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
        int waitMinutes)
    {
        return new LiveWaitHistoryObservation(
            "external-item-1",
            "mapping-1",
            observedAtUtc,
            observedAtUtc.AddSeconds(1),
            LiveOperationalStatus.Open,
            new[]
            {
                new LiveQueueObservation(LiveQueueKind.Standby, waitMinutes, false),
            },
            false);
    }
}
