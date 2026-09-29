using AmusementPark.Core.Domain.LiveData;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.LiveData;

public sealed class LiveWaitForecastCalculatorTests
{
    private static readonly DateTime CalculatedAtUtc =
        new DateTime(2026, 9, 29, 12, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Calculate_ShouldForecastNextCoveredHourFromComparableWeekdays()
    {
        LiveWaitForecastCalculator calculator = CreateCalculator();
        IReadOnlyCollection<LiveWaitHistoryObservation> observations = Enumerable.Range(1, 84)
            .Select(day => CalculatedAtUtc.Date.AddDays(-day).AddHours(13))
            .Select(timestamp => CreateObservation(
                timestamp,
                timestamp.DayOfWeek == DayOfWeek.Tuesday ? 25 : 60))
            .ToArray();

        LiveWaitForecast? forecast = calculator.Calculate(
            observations,
            CalculatedAtUtc,
            CreateActiveWindow());

        Assert.NotNull(forecast);
        Assert.Equal(new DateTime(2026, 9, 29, 13, 0, 0, DateTimeKind.Utc), forecast.ForecastFromUtc);
        Assert.Equal(25d, forecast.ExpectedWaitMinutes);
        Assert.Equal(25d, forecast.LowerBoundMinutes);
        Assert.Equal(25d, forecast.UpperBoundMinutes);
        Assert.Equal(12, forecast.TrainingDayCount);
    }

    [Fact]
    public void Calculate_ShouldNotForecastBeyondTodaysCoveredWindow()
    {
        LiveWaitForecastCalculator calculator = CreateCalculator();

        LiveWaitForecast? forecast = calculator.Calculate(
            Array.Empty<LiveWaitHistoryObservation>(),
            new DateTime(2026, 9, 29, 19, 30, 0, DateTimeKind.Utc),
            CreateActiveWindow());

        Assert.Null(forecast);
    }

    [Fact]
    public void Calculate_ShouldRejectAnInsufficientOrLateTrainingSet()
    {
        LiveWaitForecastCalculator calculator = CreateCalculator();
        List<LiveWaitHistoryObservation> observations = Enumerable.Range(1, 7)
            .Select(week => CreateObservation(
                CalculatedAtUtc.Date.AddDays(-(week * 7)).AddHours(13),
                20))
            .ToList();
        observations.Add(CreateObservation(
            CalculatedAtUtc.Date.AddDays(-56).AddHours(13),
            20,
            CalculatedAtUtc.AddMinutes(1)));

        LiveWaitForecast? forecast = calculator.Calculate(
            observations,
            CalculatedAtUtc,
            CreateActiveWindow());

        Assert.Null(forecast);
    }

    private static LiveWaitForecastCalculator CreateCalculator()
    {
        return new LiveWaitForecastCalculator(new LiveWaitForecastBacktestPolicy());
    }

    private static LivePollingActiveWindow CreateActiveWindow()
    {
        return new LivePollingActiveWindow(TimeZoneInfo.Utc, 8, 20);
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
            new[] { new LiveQueueObservation(LiveQueueKind.Standby, waitMinutes, false) },
            false);
    }
}
