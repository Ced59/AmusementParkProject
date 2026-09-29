namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveWaitHistoryStatistics
{
    public LiveWaitHistoryStatistics(
        DateTime fromUtc,
        DateTime toUtc,
        string timeZoneId,
        LiveWaitHistoryDataStatus dataStatus,
        int expectedObservationCount,
        int observationCount,
        int usableWaitCount,
        int daysCovered,
        int comparableDays,
        double coveragePercent,
        int truncatedObservationCount,
        LiveWaitHistoryExclusions exclusions,
        IReadOnlyCollection<LiveWaitHistoryHourlyStatistics> hours)
    {
        this.FromUtc = fromUtc;
        this.ToUtc = toUtc;
        this.TimeZoneId = timeZoneId;
        this.DataStatus = dataStatus;
        this.ExpectedObservationCount = expectedObservationCount;
        this.ObservationCount = observationCount;
        this.UsableWaitCount = usableWaitCount;
        this.DaysCovered = daysCovered;
        this.ComparableDays = comparableDays;
        this.CoveragePercent = coveragePercent;
        this.TruncatedObservationCount = truncatedObservationCount;
        this.Exclusions = exclusions;
        this.Hours = hours.ToArray();
    }

    public DateTime FromUtc { get; }

    public DateTime ToUtc { get; }

    public string TimeZoneId { get; }

    public LiveWaitHistoryDataStatus DataStatus { get; }

    public int ExpectedObservationCount { get; }

    public int ObservationCount { get; }

    public int UsableWaitCount { get; }

    public int DaysCovered { get; }

    public int ComparableDays { get; }

    public double CoveragePercent { get; }

    public int TruncatedObservationCount { get; }

    public LiveWaitHistoryExclusions Exclusions { get; }

    public IReadOnlyCollection<LiveWaitHistoryHourlyStatistics> Hours { get; }
}
