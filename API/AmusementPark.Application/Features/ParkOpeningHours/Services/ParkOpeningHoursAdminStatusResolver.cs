using AmusementPark.Application.Features.ParkOpeningHours.Contracts;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOpeningHours.Services;

public sealed class ParkOpeningHoursAdminStatusResolver
{
    public const int NeedsUpdateWithinDays = 30;

    public ParkOpeningHoursAdminStatus Resolve(ParkOpeningHoursScheduleSummary? summary)
    {
        return this.ResolveCoverage(summary, DateTime.UtcNow).Status;
    }

    internal ParkOpeningHoursAdminStatus Resolve(ParkOpeningHoursScheduleSummary? summary, DateTime utcNow)
    {
        return this.ResolveCoverage(summary, utcNow).Status;
    }

    public ParkOpeningHoursAdminCoverage ResolveCoverage(ParkOpeningHoursScheduleSummary? summary)
    {
        return this.ResolveCoverage(summary, DateTime.UtcNow);
    }

    internal ParkOpeningHoursAdminCoverage ResolveCoverage(ParkOpeningHoursScheduleSummary? summary, DateTime utcNow)
    {
        if (summary is null || !summary.HasScheduleData || !summary.LastDate.HasValue)
        {
            return new ParkOpeningHoursAdminCoverage
            {
                Status = ParkOpeningHoursAdminStatus.NotConfigured,
                WarningThresholdDays = NeedsUpdateWithinDays,
            };
        }

        DateOnly today = ResolveToday(summary.TimeZoneId, utcNow);
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
            DateOnly today = ResolveToday(summary.TimeZoneId, utcNow);
            return coverage.CompleteForDays.Value == 0
                && summary.LastDate.HasValue
                && summary.LastDate.Value == today.AddDays(-1);
        }

        return false;
    }

    public DateOnly ResolveLocalDate(string timeZoneId, DateTime utcNow)
    {
        return ResolveToday(timeZoneId, utcNow);
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

    private static DateOnly ResolveToday(string timeZoneId, DateTime utcNow)
    {
        TimeZoneInfo timeZone = ResolveTimeZone(timeZoneId);
        DateTime localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), timeZone);
        return DateOnly.FromDateTime(localNow);
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId.Trim(), out TimeZoneInfo? directTimeZone))
        {
            return directTimeZone;
        }

        if (!string.IsNullOrWhiteSpace(timeZoneId)
            && TimeZoneInfo.TryConvertIanaIdToWindowsId(timeZoneId.Trim(), out string? windowsTimeZoneId)
            && !string.IsNullOrWhiteSpace(windowsTimeZoneId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(windowsTimeZoneId, out TimeZoneInfo? windowsTimeZone))
        {
            return windowsTimeZone;
        }

        if (!string.IsNullOrWhiteSpace(timeZoneId)
            && TimeZoneInfo.TryConvertWindowsIdToIanaId(timeZoneId.Trim(), out string? ianaTimeZoneId)
            && !string.IsNullOrWhiteSpace(ianaTimeZoneId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(ianaTimeZoneId, out TimeZoneInfo? ianaTimeZone))
        {
            return ianaTimeZone;
        }

        return TimeZoneInfo.Utc;
    }
}
