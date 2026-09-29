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

        DateTime forecastFromUtc = ResolveFollowingLocalHourBoundary(
            calculatedAtUtc,
            activeWindow.TimeZone);
        DateTime forecastToUtc = ResolveFollowingLocalHourBoundary(
            forecastFromUtc,
            activeWindow.TimeZone);
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
            forecastToUtc,
            calculatedAtUtc,
            Math.Round(LiveWaitForecastObservationSeries.Percentile(comparableWaits, 0.50d), 1),
            Math.Round(LiveWaitForecastObservationSeries.Percentile(comparableWaits, 0.10d), 1),
            Math.Round(LiveWaitForecastObservationSeries.Percentile(comparableWaits, 0.90d), 1),
            comparableWaits.Length);
    }

    private static DateTime ResolveFollowingLocalHourBoundary(
        DateTime timestampUtc,
        TimeZoneInfo timeZone)
    {
        DateTime timestampLocal = TimeZoneInfo.ConvertTimeFromUtc(timestampUtc, timeZone);
        DateTime localBoundary = DateTime.SpecifyKind(
            new DateTime(
                timestampLocal.Year,
                timestampLocal.Month,
                timestampLocal.Day,
                timestampLocal.Hour,
                0,
                0).AddHours(1),
            DateTimeKind.Unspecified);

        for (int candidateIndex = 0; candidateIndex < 24; candidateIndex++)
        {
            if (timeZone.IsInvalidTime(localBoundary))
            {
                localBoundary = localBoundary.AddHours(1);
                continue;
            }

            DateTime[] candidatesUtc = timeZone.IsAmbiguousTime(localBoundary)
                ? timeZone.GetAmbiguousTimeOffsets(localBoundary)
                    .Select(offset => new DateTimeOffset(localBoundary, offset).UtcDateTime)
                    .OrderDescending()
                    .ToArray()
                : new[] { TimeZoneInfo.ConvertTimeToUtc(localBoundary, timeZone) };
            foreach (DateTime candidateUtc in candidatesUtc)
            {
                if (candidateUtc > timestampUtc)
                {
                    return candidateUtc;
                }
            }

            localBoundary = localBoundary.AddHours(1);
        }

        throw new InvalidOperationException(
            "A following local-hour boundary could not be resolved for the configured time zone.");
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("A forecast timestamp must be UTC.", parameterName);
        }
    }
}
