namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveWaitForecastCalculator
{
    private readonly LiveWaitForecastBacktestPolicy policy;

    public LiveWaitForecastCalculator(LiveWaitForecastBacktestPolicy policy)
    {
        this.policy = policy;
    }

    public LiveWaitForecast? Calculate(
        IReadOnlyCollection<LiveWaitHistoryObservation> observations,
        DateTime calculatedAtUtc,
        LivePollingActiveWindow activeWindow)
    {
        ArgumentNullException.ThrowIfNull(observations);
        ArgumentNullException.ThrowIfNull(activeWindow);
        EnsureUtc(calculatedAtUtc, nameof(calculatedAtUtc));

        DateTime forecastFromUtc = NextWholeHour(calculatedAtUtc);
        DateTime calculatedLocal = TimeZoneInfo.ConvertTimeFromUtc(
            calculatedAtUtc,
            activeWindow.TimeZone);
        DateTime forecastLocal = TimeZoneInfo.ConvertTimeFromUtc(
            forecastFromUtc,
            activeWindow.TimeZone);
        if (DateOnly.FromDateTime(calculatedLocal) != DateOnly.FromDateTime(forecastLocal)
            || !activeWindow.Contains(forecastFromUtc))
        {
            return null;
        }

        DateTime trainingStartsAtUtc = forecastFromUtc.AddDays(-this.policy.TrainingWindowDays);
        double[] comparableWaits = LiveWaitForecastObservationSeries
            .Build(observations, calculatedAtUtc, calculatedAtUtc, activeWindow)
            .Where(point => point.AvailableAtUtc <= calculatedAtUtc
                && point.TimestampUtc >= trainingStartsAtUtc
                && point.DayOfWeek == DateOnly.FromDateTime(forecastLocal).DayOfWeek
                && point.LocalHour == forecastLocal.Hour)
            .Select(static point => point.WaitMinutes)
            .Order()
            .ToArray();
        if (comparableWaits.Length < this.policy.MinimumCandidateTrainingDays)
        {
            return null;
        }

        return new LiveWaitForecast(
            forecastFromUtc,
            forecastFromUtc.AddHours(1),
            calculatedAtUtc,
            Math.Round(LiveWaitForecastObservationSeries.Percentile(comparableWaits, 0.50d), 1),
            Math.Round(LiveWaitForecastObservationSeries.Percentile(comparableWaits, 0.10d), 1),
            Math.Round(LiveWaitForecastObservationSeries.Percentile(comparableWaits, 0.90d), 1),
            comparableWaits.Length);
    }

    private static DateTime NextWholeHour(DateTime timestampUtc)
    {
        long hourTicks = TimeSpan.TicksPerHour;
        long nextHourTicks = ((timestampUtc.Ticks / hourTicks) + 1) * hourTicks;
        return new DateTime(nextHourTicks, DateTimeKind.Utc);
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("A forecast timestamp must be UTC.", parameterName);
        }
    }
}
