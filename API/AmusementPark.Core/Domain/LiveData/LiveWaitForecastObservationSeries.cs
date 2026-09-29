namespace AmusementPark.Core.Domain.LiveData;

internal static class LiveWaitForecastObservationSeries
{
    public static LiveWaitForecastHourlyPoint[] Build(
        IReadOnlyCollection<LiveWaitHistoryObservation> observations,
        DateTime observedBeforeUtc,
        DateTime receivedByUtc,
        LivePollingActiveWindow activeWindow)
    {
        ArgumentNullException.ThrowIfNull(observations);
        ArgumentNullException.ThrowIfNull(activeWindow);

        LiveWaitHistoryObservation[] uniqueObservations = observations
            .Where(observation => observation.ObservedAtUtc < observedBeforeUtc
                && observation.ReceivedAtUtc <= receivedByUtc)
            .GroupBy(static observation => observation.ObservedAtUtc)
            .Select(static group => group
                .OrderByDescending(static observation => observation.ReceivedAtUtc)
                .First())
            .OrderBy(static observation => observation.ObservedAtUtc)
            .ToArray();

        return uniqueObservations
            .Where(observation => !observation.IsBucketTruncated
                && activeWindow.Contains(observation.ObservedAtUtc)
                && IsOperating(observation.Status))
            .Select(observation => (
                Observation: observation,
                Wait: observation.Queues
                    .Where(static queue => queue.Kind == LiveQueueKind.Standby)
                    .Select(static queue => queue.WaitTimeMinutes)
                    .FirstOrDefault(static value => value.HasValue)))
            .Where(static value => value.Wait.HasValue)
            .Select(value => (
                value.Observation,
                Wait: value.Wait!.Value,
                Local: TimeZoneInfo.ConvertTimeFromUtc(
                    value.Observation.ObservedAtUtc,
                    activeWindow.TimeZone)))
            .GroupBy(static value => (DateOnly.FromDateTime(value.Local), value.Local.Hour))
            .Select(static group => new LiveWaitForecastHourlyPoint(
                group.Min(static value => value.Observation.ObservedAtUtc),
                group.Max(static value => value.Observation.ReceivedAtUtc),
                group.Key.Item1,
                group.Key.Item1.DayOfWeek,
                group.Key.Hour,
                Percentile(
                    group.Select(static value => (double)value.Wait).Order().ToArray(),
                    0.50d)))
            .OrderBy(static point => point.TimestampUtc)
            .ToArray();
    }

    public static double Percentile(IReadOnlyList<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0)
        {
            return 0d;
        }

        double index = (sortedValues.Count - 1) * percentile;
        int lowerIndex = (int)Math.Floor(index);
        int upperIndex = (int)Math.Ceiling(index);
        return sortedValues[lowerIndex]
            + ((sortedValues[upperIndex] - sortedValues[lowerIndex]) * (index - lowerIndex));
    }

    private static bool IsOperating(LiveOperationalStatus status)
    {
        return status is LiveOperationalStatus.Open
            or LiveOperationalStatus.OperatingWithLimitations;
    }
}
