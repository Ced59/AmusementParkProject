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

    [Fact]
    public void Calculate_ShouldRoundAtTheNextLocalHourForAHalfHourOffset()
    {
        TimeZoneInfo india = TimeZoneInfo.CreateCustomTimeZone(
            "India-test",
            TimeSpan.FromHours(5.5d),
            "India-test",
            "India-test");
        DateTime calculatedAtUtc =
            new DateTime(2026, 9, 29, 12, 20, 0, DateTimeKind.Utc);
        IReadOnlyCollection<LiveWaitHistoryObservation> observations = Enumerable.Range(1, 8)
            .Select(week => CreateObservation(
                new DateTime(2026, 9, 29, 12, 30, 0, DateTimeKind.Utc).AddDays(-(week * 7)),
                25))
            .ToArray();

        LiveWaitForecast? forecast = CreateCalculator().Calculate(
            observations,
            calculatedAtUtc,
            new LivePollingActiveWindow(india, 0, 24));

        Assert.NotNull(forecast);
        Assert.Equal(
            new DateTime(2026, 9, 29, 12, 30, 0, DateTimeKind.Utc),
            forecast.ForecastFromUtc);
        Assert.Equal(
            new DateTime(2026, 9, 29, 13, 30, 0, DateTimeKind.Utc),
            forecast.ForecastToUtc);
    }

    [Fact]
    public void Calculate_ShouldChooseTheLaterRepeatedHourAtDaylightSavingFallback()
    {
        TimeZoneInfo paris = CreateParisTestTimeZone();
        DateTime calculatedAtUtc =
            new DateTime(2026, 10, 24, 23, 30, 0, DateTimeKind.Utc);
        DateTime calculatedLocal = TimeZoneInfo.ConvertTimeFromUtc(calculatedAtUtc, paris);
        IReadOnlyCollection<LiveWaitHistoryObservation> observations = Enumerable.Range(1, 8)
            .Select(week => calculatedLocal.Date.AddDays(-(week * 7)).AddHours(2).AddMinutes(15))
            .Select(local => TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(local, DateTimeKind.Unspecified),
                paris))
            .Select(timestamp => CreateObservation(timestamp, 25))
            .ToArray();

        LiveWaitForecast? forecast = CreateCalculator().Calculate(
            observations,
            calculatedAtUtc,
            new LivePollingActiveWindow(paris, 0, 24));

        Assert.NotNull(forecast);
        Assert.Equal(
            new DateTime(2026, 10, 25, 1, 0, 0, DateTimeKind.Utc),
            forecast.ForecastFromUtc);
        Assert.Equal(
            new DateTime(2026, 10, 25, 2, 0, 0, DateTimeKind.Utc),
            forecast.ForecastToUtc);
        Assert.Equal(2, TimeZoneInfo.ConvertTimeFromUtc(forecast.ForecastFromUtc, paris).Hour);
        Assert.Equal(3, TimeZoneInfo.ConvertTimeFromUtc(forecast.ForecastToUtc, paris).Hour);
    }

    [Fact]
    public void Calculate_ShouldSkipTheMissingHourAtDaylightSavingStart()
    {
        TimeZoneInfo paris = CreateParisTestTimeZone();
        DateTime calculatedAtUtc =
            new DateTime(2026, 3, 29, 0, 30, 0, DateTimeKind.Utc);
        DateTime calculatedLocal = TimeZoneInfo.ConvertTimeFromUtc(calculatedAtUtc, paris);
        IReadOnlyCollection<LiveWaitHistoryObservation> observations = Enumerable.Range(1, 8)
            .Select(week => calculatedLocal.Date.AddDays(-(week * 7)).AddHours(3).AddMinutes(15))
            .Select(local => TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(local, DateTimeKind.Unspecified),
                paris))
            .Select(timestamp => CreateObservation(timestamp, 25))
            .ToArray();

        LiveWaitForecast? forecast = CreateCalculator().Calculate(
            observations,
            calculatedAtUtc,
            new LivePollingActiveWindow(paris, 0, 24));

        Assert.NotNull(forecast);
        Assert.Equal(
            new DateTime(2026, 3, 29, 1, 0, 0, DateTimeKind.Utc),
            forecast.ForecastFromUtc);
        Assert.Equal(
            new DateTime(2026, 3, 29, 2, 0, 0, DateTimeKind.Utc),
            forecast.ForecastToUtc);
        Assert.Equal(3, TimeZoneInfo.ConvertTimeFromUtc(forecast.ForecastFromUtc, paris).Hour);
        Assert.Equal(4, TimeZoneInfo.ConvertTimeFromUtc(forecast.ForecastToUtc, paris).Hour);
    }

    private static LiveWaitForecastCalculator CreateCalculator()
    {
        return new LiveWaitForecastCalculator(new LiveWaitForecastBacktestPolicy());
    }

    private static LivePollingActiveWindow CreateActiveWindow()
    {
        return new LivePollingActiveWindow(TimeZoneInfo.Utc, 8, 20);
    }

    private static TimeZoneInfo CreateParisTestTimeZone()
    {
        TimeZoneInfo.TransitionTime daylightStarts = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 2, 0, 0),
            3,
            5,
            DayOfWeek.Sunday);
        TimeZoneInfo.TransitionTime daylightEnds = TimeZoneInfo.TransitionTime.CreateFloatingDateRule(
            new DateTime(1, 1, 1, 3, 0, 0),
            10,
            5,
            DayOfWeek.Sunday);
        TimeZoneInfo.AdjustmentRule rule = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(
            new DateTime(2026, 1, 1),
            new DateTime(2026, 12, 31),
            TimeSpan.FromHours(1),
            daylightStarts,
            daylightEnds);
        return TimeZoneInfo.CreateCustomTimeZone(
            "Europe-Paris-test",
            TimeSpan.FromHours(1),
            "Europe-Paris-test",
            "Europe-Paris-test",
            "Europe-Paris-test-daylight",
            new[] { rule });
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
