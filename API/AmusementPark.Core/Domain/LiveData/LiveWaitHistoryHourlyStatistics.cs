namespace AmusementPark.Core.Domain.LiveData;

public sealed class LiveWaitHistoryHourlyStatistics
{
    public LiveWaitHistoryHourlyStatistics(
        int localHour,
        LiveWaitHistoryDataStatus dataStatus,
        int expectedObservationCount,
        int observationCount,
        int usableWaitCount,
        int daysCovered,
        int comparableDays,
        double coveragePercent,
        double? robustMinimumMinutes,
        double? firstQuartileMinutes,
        double? medianMinutes,
        double? thirdQuartileMinutes,
        double? robustMaximumMinutes)
    {
        if (localHour is < 0 or > 23)
        {
            throw new ArgumentOutOfRangeException(nameof(localHour));
        }

        this.LocalHour = localHour;
        this.DataStatus = dataStatus;
        this.ExpectedObservationCount = expectedObservationCount;
        this.ObservationCount = observationCount;
        this.UsableWaitCount = usableWaitCount;
        this.DaysCovered = daysCovered;
        this.ComparableDays = comparableDays;
        this.CoveragePercent = coveragePercent;
        this.RobustMinimumMinutes = robustMinimumMinutes;
        this.FirstQuartileMinutes = firstQuartileMinutes;
        this.MedianMinutes = medianMinutes;
        this.ThirdQuartileMinutes = thirdQuartileMinutes;
        this.RobustMaximumMinutes = robustMaximumMinutes;
    }

    public int LocalHour { get; }

    public LiveWaitHistoryDataStatus DataStatus { get; }

    public int ExpectedObservationCount { get; }

    public int ObservationCount { get; }

    public int UsableWaitCount { get; }

    public int DaysCovered { get; }

    public int ComparableDays { get; }

    public double CoveragePercent { get; }

    public double? RobustMinimumMinutes { get; }

    public double? FirstQuartileMinutes { get; }

    public double? MedianMinutes { get; }

    public double? ThirdQuartileMinutes { get; }

    public double? RobustMaximumMinutes { get; }
}
