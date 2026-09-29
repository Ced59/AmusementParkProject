using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.LiveData;

public sealed class LiveWaitHistoryStatisticsCalculatorTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;

    [Fact]
    public void Calculate_UsesMedianQuartilesAndRobustBoundsPerLocalHour()
    {
        LiveWaitHistoryStatisticsCalculator calculator = new LiveWaitHistoryStatisticsCalculator();
        DateTime fromUtc = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
        List<LiveWaitHistoryObservation> observations = Enumerable.Range(0, 10)
            .Select(index => CreateObservation(
                fromUtc.AddDays(index % 5).AddMinutes(index * 5),
                index * 10))
            .ToList();

        LiveWaitHistoryStatistics result = calculator.Calculate(
            observations,
            fromUtc,
            new DateTime(2026, 9, 6, 9, 0, 0, DateTimeKind.Utc),
            new LivePollingActiveWindow(Utc, 8, 9),
            TimeSpan.FromMinutes(5));

        LiveWaitHistoryHourlyStatistics hour = Assert.Single(result.Hours);
        Assert.Equal(10, hour.UsableWaitCount);
        Assert.Equal(45d, hour.MedianMinutes);
        Assert.Equal(22.5d, hour.FirstQuartileMinutes);
        Assert.Equal(67.5d, hour.ThirdQuartileMinutes);
        Assert.Equal(9d, hour.RobustMinimumMinutes);
        Assert.Equal(81d, hour.RobustMaximumMinutes);
        Assert.Equal(5, hour.ComparableDays);
        Assert.Equal(LiveWaitHistoryDataStatus.Sparse, hour.DataStatus);
    }

    [Fact]
    public void Calculate_DeduplicatesTimestampsAndKeepsLatestReceivedObservation()
    {
        LiveWaitHistoryStatisticsCalculator calculator = new LiveWaitHistoryStatisticsCalculator();
        DateTime observedAtUtc = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
        LiveWaitHistoryObservation older = CreateObservation(observedAtUtc, 10);
        LiveWaitHistoryObservation newer = CreateObservation(
            observedAtUtc,
            20,
            observedAtUtc.AddMinutes(2));

        LiveWaitHistoryStatistics result = calculator.Calculate(
            new[] { older, newer },
            observedAtUtc,
            observedAtUtc.AddHours(1),
            new LivePollingActiveWindow(Utc, 8, 9),
            TimeSpan.FromMinutes(5));

        Assert.Equal(1, result.ObservationCount);
        Assert.Equal(1, result.Exclusions.DuplicateObservations);
        Assert.Null(Assert.Single(result.Hours).MedianMinutes);
    }

    [Fact]
    public void Calculate_ReportsGapsAndExclusionsWithoutInventingStatistics()
    {
        LiveWaitHistoryStatisticsCalculator calculator = new LiveWaitHistoryStatisticsCalculator();
        DateTime fromUtc = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
        LiveWaitHistoryObservation outsideWindow = CreateObservation(fromUtc.AddHours(-1), 15);
        LiveWaitHistoryObservation closed = CreateObservation(
            fromUtc,
            20,
            fromUtc,
            LiveOperationalStatus.Closed);
        LiveWaitHistoryObservation withoutWait = new LiveWaitHistoryObservation(
            "external-item-1",
            "mapping-1",
            fromUtc.AddMinutes(5),
            fromUtc.AddMinutes(5),
            LiveOperationalStatus.Open,
            Array.Empty<LiveQueueObservation>(),
            false);

        LiveWaitHistoryStatistics result = calculator.Calculate(
            new[] { outsideWindow, closed, withoutWait },
            fromUtc.AddHours(-1),
            fromUtc.AddHours(2),
            new LivePollingActiveWindow(Utc, 8, 10),
            TimeSpan.FromMinutes(5));

        Assert.Equal(LiveWaitHistoryDataStatus.Insufficient, result.DataStatus);
        Assert.Equal(2, result.ObservationCount);
        Assert.Equal(0, result.UsableWaitCount);
        Assert.Equal(1, result.Exclusions.OutsideActiveWindow);
        Assert.Equal(1, result.Exclusions.NonOperatingStatus);
        Assert.Equal(1, result.Exclusions.MissingStandbyWait);
        Assert.All(result.Hours, hour => Assert.Null(hour.MedianMinutes));
    }

    [Fact]
    public void Calculate_UsesConfiguredTimeZoneForHourlyGrouping()
    {
        TimeZoneInfo paris = TimeZoneInfo.CreateCustomTimeZone(
            "Europe/Paris-test",
            TimeSpan.FromHours(2),
            "Europe/Paris-test",
            "Europe/Paris-test");
        DateTime observedAtUtc = new DateTime(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

        LiveWaitHistoryStatistics result = new LiveWaitHistoryStatisticsCalculator().Calculate(
            new[] { CreateObservation(observedAtUtc, 30) },
            observedAtUtc,
            observedAtUtc.AddHours(1),
            new LivePollingActiveWindow(paris, 10, 11),
            TimeSpan.FromMinutes(5));

        LiveWaitHistoryHourlyStatistics hour = Assert.Single(result.Hours);
        Assert.Equal(10, hour.LocalHour);
        Assert.Equal(1, hour.UsableWaitCount);
        Assert.Null(hour.MedianMinutes);
        Assert.Equal("Europe/Paris-test", result.TimeZoneId);
    }

    private static LiveWaitHistoryObservation CreateObservation(
        DateTime observedAtUtc,
        int waitMinutes,
        DateTime? receivedAtUtc = null,
        LiveOperationalStatus status = LiveOperationalStatus.Open)
    {
        return new LiveWaitHistoryObservation(
            "external-item-1",
            "mapping-1",
            observedAtUtc,
            receivedAtUtc ?? observedAtUtc,
            status,
            new[]
            {
                new LiveQueueObservation(
                    LiveQueueKind.Standby,
                    waitMinutes,
                    false),
            },
            false);
    }
}
