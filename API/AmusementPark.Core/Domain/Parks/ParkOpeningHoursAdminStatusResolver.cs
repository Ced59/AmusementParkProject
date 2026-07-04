namespace AmusementPark.Core.Domain.Parks;

public sealed class ParkOpeningHoursAdminStatusResolver
{
    public const int NeedsUpdateWithinDays = 30;

    public ParkOpeningHoursAdminStatus Resolve(ParkOpeningHoursScheduleSummary? summary)
    {
        return this.ResolveCoverage(summary, DateTime.UtcNow).Status;
    }

    public ParkOpeningHoursAdminStatus Resolve(ParkOpeningHoursScheduleSummary? summary, DateTime utcNow)
    {
        return this.ResolveCoverage(summary, utcNow).Status;
    }

    public ParkOpeningHoursAdminCoverage ResolveCoverage(ParkOpeningHoursScheduleSummary? summary)
    {
        return this.ResolveCoverage(summary, DateTime.UtcNow);
    }

    public ParkOpeningHoursAdminCoverage ResolveCoverage(ParkOpeningHoursScheduleSummary? summary, DateTime utcNow)
    {
        if (summary is null || !summary.HasScheduleData || !summary.LastDate.HasValue)
        {
            return new ParkOpeningHoursAdminCoverage
            {
                Status = ParkOpeningHoursAdminStatus.NotConfigured,
                WarningThresholdDays = NeedsUpdateWithinDays,
            };
        }

        DateOnly today = ParkOpeningHoursTimeZoneResolver.ResolveLocalDate(summary.TimeZoneId, utcNow);
        DateOnly? completeUntilDate = ResolveCompleteUntilDate(summary, today);
        int completeForDays = completeUntilDate.HasValue && completeUntilDate.Value >= today
            ? completeUntilDate.Value.DayNumber - today.DayNumber + 1
            : 0;

        ParkOpeningHoursAdminStatus status = ResolveStatus(summary, today, completeForDays);
        return new ParkOpeningHoursAdminCoverage
        {
            Status = status,
            CompleteForDays = completeForDays,
            CompleteUntilDate = completeUntilDate,
            WarningThresholdDays = NeedsUpdateWithinDays,
        };
    }

    public bool IsCoverageNotificationThresholdReached(ParkOpeningHoursScheduleSummary summary, int thresholdDays, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(summary);

        ParkOpeningHoursAdminCoverage coverage = this.ResolveCoverage(summary, utcNow);
        if (!coverage.CompleteForDays.HasValue)
        {
            return false;
        }

        if (thresholdDays == NeedsUpdateWithinDays)
        {
            return coverage.CompleteForDays.Value == NeedsUpdateWithinDays;
        }

        if (thresholdDays == 0)
        {
            DateOnly today = ParkOpeningHoursTimeZoneResolver.ResolveLocalDate(summary.TimeZoneId, utcNow);
            return coverage.CompleteForDays.Value == 0
                && summary.LastDate.HasValue
                && summary.LastDate.Value == today.AddDays(-1);
        }

        return false;
    }

    public DateOnly ResolveLocalDate(string timeZoneId, DateTime utcNow)
    {
        return ParkOpeningHoursTimeZoneResolver.ResolveLocalDate(timeZoneId, utcNow);
    }

    private static DateOnly? ResolveCompleteUntilDate(ParkOpeningHoursScheduleSummary summary, DateOnly today)
    {
        if (summary.CoverageSegments.Count > 0)
        {
            ParkOpeningHoursCoverageSegmentSummary? currentSegment = summary.CoverageSegments
                .Where(segment => segment.StartDate <= today && segment.EndDate >= today)
                .OrderByDescending(static segment => segment.EndDate)
                .FirstOrDefault();

            return currentSegment?.EndDate;
        }

        return summary.LastDate;
    }

    private static ParkOpeningHoursAdminStatus ResolveStatus(ParkOpeningHoursScheduleSummary summary, DateOnly today, int completeForDays)
    {
        if (completeForDays == 0)
        {
            return summary.LastDate.HasValue && summary.LastDate.Value < today
                ? ParkOpeningHoursAdminStatus.Expired
                : ParkOpeningHoursAdminStatus.NeedsUpdate;
        }

        if (completeForDays <= NeedsUpdateWithinDays)
        {
            return ParkOpeningHoursAdminStatus.NeedsUpdate;
        }

        return ParkOpeningHoursAdminStatus.UpToDate;
    }
}
