namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalCoverage
{
    public HistoricalCoverage(
        int totalSubjectCount,
        int reliablePeriodSubjectCount,
        int partialPeriodSubjectCount,
        int undatedSubjectCount,
        HistoricalFieldCoverage nameCoverage,
        HistoricalFieldCoverage zoneCoverage,
        DateTime? lastReviewedAtUtc,
        HistoricalCoverageStatus status)
    {
        if (totalSubjectCount < 0
            || reliablePeriodSubjectCount < 0
            || partialPeriodSubjectCount < 0
            || undatedSubjectCount < 0
            || reliablePeriodSubjectCount + partialPeriodSubjectCount + undatedSubjectCount
                != totalSubjectCount)
        {
            throw new ArgumentOutOfRangeException(nameof(totalSubjectCount));
        }

        ArgumentNullException.ThrowIfNull(nameCoverage);
        ArgumentNullException.ThrowIfNull(zoneCoverage);
        if (nameCoverage.ApplicableSubjectCount != totalSubjectCount
            || zoneCoverage.ApplicableSubjectCount > totalSubjectCount)
        {
            throw new ArgumentException("Historical field coverage is inconsistent with its subjects.");
        }

        if (lastReviewedAtUtc.HasValue && lastReviewedAtUtc.Value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The historical coverage review timestamp must be UTC.",
                nameof(lastReviewedAtUtc));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        this.TotalSubjectCount = totalSubjectCount;
        this.ReliablePeriodSubjectCount = reliablePeriodSubjectCount;
        this.PartialPeriodSubjectCount = partialPeriodSubjectCount;
        this.UndatedSubjectCount = undatedSubjectCount;
        this.NameCoverage = nameCoverage;
        this.ZoneCoverage = zoneCoverage;
        this.LastReviewedAtUtc = lastReviewedAtUtc;
        this.Status = status;
    }

    public int TotalSubjectCount { get; }

    public int ReliablePeriodSubjectCount { get; }

    public int PartialPeriodSubjectCount { get; }

    public int UndatedSubjectCount { get; }

    public HistoricalFieldCoverage NameCoverage { get; }

    public HistoricalFieldCoverage ZoneCoverage { get; }

    public DateTime? LastReviewedAtUtc { get; }

    public HistoricalCoverageStatus Status { get; }
}
