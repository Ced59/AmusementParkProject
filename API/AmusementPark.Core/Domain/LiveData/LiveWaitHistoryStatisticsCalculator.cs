namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveWaitHistoryStatisticsCalculator
{
    private const int MinimumUsableObservations = 12;
    private const int MinimumUsableObservationsPerHour = 5;
    private const int MinimumComparableDays = 2;
    private const int HealthyComparableDays = 5;
    private const double HealthyCoveragePercent = 60d;

    public LiveWaitHistoryStatistics Calculate(
        IReadOnlyCollection<LiveWaitHistoryObservation> observations,
        DateTime fromUtc,
        DateTime toUtc,
        LivePollingActiveWindow activeWindow,
        TimeSpan pollingInterval)
    {
        ArgumentNullException.ThrowIfNull(observations);
        ArgumentNullException.ThrowIfNull(activeWindow);
        EnsureUtc(fromUtc, nameof(fromUtc));
        EnsureUtc(toUtc, nameof(toUtc));
        if (fromUtc >= toUtc)
        {
            throw new ArgumentException("The live history period must have a positive duration.");
        }

        if (pollingInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pollingInterval));
        }

        Dictionary<int, int> expectedByHour = BuildExpectedObservationsByHour(
            fromUtc,
            toUtc,
            activeWindow,
            pollingInterval);
        LiveWaitHistoryObservation[] periodObservations = observations
            .Where(observation => observation.ObservedAtUtc >= fromUtc
                && observation.ObservedAtUtc < toUtc)
            .ToArray();
        LiveWaitHistoryObservation[] uniqueObservations = periodObservations
            .GroupBy(static observation => observation.ObservedAtUtc)
            .Select(static group => group
                .OrderByDescending(static observation => observation.ReceivedAtUtc)
                .First())
            .OrderBy(static observation => observation.ObservedAtUtc)
            .ToArray();

        int duplicateCount = periodObservations.Length - uniqueObservations.Length;
        List<LiveWaitHistoryObservation> activeObservations = new List<LiveWaitHistoryObservation>();
        int outsideActiveWindow = 0;
        foreach (LiveWaitHistoryObservation observation in uniqueObservations)
        {
            if (!activeWindow.Contains(observation.ObservedAtUtc))
            {
                outsideActiveWindow++;
                continue;
            }

            activeObservations.Add(observation);
        }

        List<(LiveWaitHistoryObservation Observation, int LocalHour, DateOnly LocalDate, int Wait)> usable =
            new List<(LiveWaitHistoryObservation Observation, int LocalHour, DateOnly LocalDate, int Wait)>();
        int nonOperatingStatus = 0;
        int missingStandbyWait = 0;
        foreach (LiveWaitHistoryObservation observation in activeObservations)
        {
            DateTime local = TimeZoneInfo.ConvertTimeFromUtc(
                observation.ObservedAtUtc,
                activeWindow.TimeZone);
            if (!IsOperating(observation.Status))
            {
                nonOperatingStatus++;
                continue;
            }

            int? wait = observation.Queues
                .Where(static queue => queue.Kind == LiveQueueKind.Standby)
                .Select(static queue => queue.WaitTimeMinutes)
                .FirstOrDefault(static value => value.HasValue);
            if (!wait.HasValue)
            {
                missingStandbyWait++;
                continue;
            }

            usable.Add((
                observation,
                local.Hour,
                DateOnly.FromDateTime(local),
                wait.Value));
        }

        int expectedCount = expectedByHour.Values.Sum();
        int observationCount = activeObservations.Count;
        int daysCovered = activeObservations
            .Select(observation => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
                observation.ObservedAtUtc,
                activeWindow.TimeZone)))
            .Distinct()
            .Count();
        int comparableDays = usable.Select(static value => value.LocalDate).Distinct().Count();
        int truncatedObservationCount = activeObservations.Count(static observation =>
            observation.IsBucketTruncated);
        double coveragePercent = Percentage(observationCount, expectedCount);
        LiveWaitHistoryDataStatus dataStatus = ResolveStatus(
            observationCount,
            usable.Count,
            comparableDays,
            coveragePercent,
            truncatedObservationCount,
            MinimumUsableObservations);

        List<LiveWaitHistoryHourlyStatistics> hours = new List<LiveWaitHistoryHourlyStatistics>();
        for (int hour = activeWindow.StartsAtHour; hour < activeWindow.EndsAtHour; hour++)
        {
            LiveWaitHistoryObservation[] hourObservations = activeObservations
                .Where(observation => TimeZoneInfo.ConvertTimeFromUtc(
                    observation.ObservedAtUtc,
                    activeWindow.TimeZone).Hour == hour)
                .ToArray();
            (LiveWaitHistoryObservation Observation, int LocalHour, DateOnly LocalDate, int Wait)[] hourUsable =
                usable.Where(value => value.LocalHour == hour).ToArray();
            int hourExpected = expectedByHour.GetValueOrDefault(hour);
            int hourDaysCovered = hourObservations
                .Select(observation => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
                    observation.ObservedAtUtc,
                    activeWindow.TimeZone)))
                .Distinct()
                .Count();
            int hourComparableDays = hourUsable.Select(static value => value.LocalDate).Distinct().Count();
            int hourTruncated = hourObservations.Count(static observation => observation.IsBucketTruncated);
            double hourCoverage = Percentage(hourObservations.Length, hourExpected);
            LiveWaitHistoryDataStatus hourStatus = ResolveStatus(
                hourObservations.Length,
                hourUsable.Length,
                hourComparableDays,
                hourCoverage,
                hourTruncated,
                MinimumUsableObservationsPerHour);
            int[] waits = hourStatus is LiveWaitHistoryDataStatus.Sparse
                or LiveWaitHistoryDataStatus.Usable
                ? hourUsable.Select(static value => value.Wait).Order().ToArray()
                : Array.Empty<int>();
            hours.Add(new LiveWaitHistoryHourlyStatistics(
                hour,
                hourStatus,
                hourExpected,
                hourObservations.Length,
                hourUsable.Length,
                hourDaysCovered,
                hourComparableDays,
                hourCoverage,
                Percentile(waits, 0.10d),
                Percentile(waits, 0.25d),
                Percentile(waits, 0.50d),
                Percentile(waits, 0.75d),
                Percentile(waits, 0.90d)));
        }

        return new LiveWaitHistoryStatistics(
            fromUtc,
            toUtc,
            activeWindow.TimeZone.Id,
            dataStatus,
            expectedCount,
            observationCount,
            usable.Count,
            daysCovered,
            comparableDays,
            coveragePercent,
            truncatedObservationCount,
            new LiveWaitHistoryExclusions(
                duplicateCount,
                outsideActiveWindow,
                nonOperatingStatus,
                missingStandbyWait),
            hours.AsReadOnly());
    }

    private static Dictionary<int, int> BuildExpectedObservationsByHour(
        DateTime fromUtc,
        DateTime toUtc,
        LivePollingActiveWindow activeWindow,
        TimeSpan pollingInterval)
    {
        Dictionary<int, int> expectedByHour = new Dictionary<int, int>();
        long remainder = fromUtc.Ticks % pollingInterval.Ticks;
        DateTime cursor = remainder == 0
            ? fromUtc
            : new DateTime(
                checked(fromUtc.Ticks + pollingInterval.Ticks - remainder),
                DateTimeKind.Utc);
        while (cursor < toUtc)
        {
            if (activeWindow.Contains(cursor))
            {
                int localHour = TimeZoneInfo.ConvertTimeFromUtc(cursor, activeWindow.TimeZone).Hour;
                expectedByHour[localHour] = expectedByHour.GetValueOrDefault(localHour) + 1;
            }

            if (DateTime.MaxValue.Ticks - cursor.Ticks < pollingInterval.Ticks)
            {
                break;
            }

            cursor = cursor.Add(pollingInterval);
        }

        return expectedByHour;
    }

    private static LiveWaitHistoryDataStatus ResolveStatus(
        int observationCount,
        int usableWaitCount,
        int comparableDays,
        double coveragePercent,
        int truncatedObservationCount,
        int minimumUsableObservations)
    {
        if (observationCount == 0)
        {
            return LiveWaitHistoryDataStatus.Unavailable;
        }

        if (usableWaitCount < minimumUsableObservations
            || comparableDays < MinimumComparableDays)
        {
            return LiveWaitHistoryDataStatus.Insufficient;
        }

        if (coveragePercent < HealthyCoveragePercent
            || comparableDays < HealthyComparableDays
            || truncatedObservationCount > 0)
        {
            return LiveWaitHistoryDataStatus.Sparse;
        }

        return LiveWaitHistoryDataStatus.Usable;
    }

    private static bool IsOperating(LiveOperationalStatus status)
    {
        return status is LiveOperationalStatus.Open
            or LiveOperationalStatus.OperatingWithLimitations;
    }

    private static double Percentage(int numerator, int denominator)
    {
        if (denominator <= 0)
        {
            return 0d;
        }

        return Math.Round(Math.Min(100d, numerator * 100d / denominator), 1);
    }

    private static double? Percentile(IReadOnlyList<int> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0)
        {
            return null;
        }

        double index = (sortedValues.Count - 1) * percentile;
        int lowerIndex = (int)Math.Floor(index);
        int upperIndex = (int)Math.Ceiling(index);
        double value = sortedValues[lowerIndex]
            + ((sortedValues[upperIndex] - sortedValues[lowerIndex]) * (index - lowerIndex));
        return Math.Round(value, 1);
    }

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("A live history period must be UTC.", parameterName);
        }
    }
}
