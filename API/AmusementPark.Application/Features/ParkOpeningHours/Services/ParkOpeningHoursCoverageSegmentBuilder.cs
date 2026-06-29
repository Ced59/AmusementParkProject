using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOpeningHours.Services;

public sealed class ParkOpeningHoursCoverageSegmentBuilder
{
    private const int MaximumCoverageDayCount = 1096;

    public IReadOnlyCollection<ParkOpeningHoursCoverageSegment> BuildSegments(ParkOpeningHoursSchedule schedule)
    {
        return this.BuildSegments(schedule, DateTime.UtcNow);
    }

    internal IReadOnlyCollection<ParkOpeningHoursCoverageSegment> BuildSegments(ParkOpeningHoursSchedule schedule, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        DateOnly? firstDate = ResolveFirstDate(schedule);
        DateOnly? lastDate = ResolveLastDate(schedule);
        if (!firstDate.HasValue || !lastDate.HasValue)
        {
            return Array.Empty<ParkOpeningHoursCoverageSegment>();
        }

        DateOnly today = ResolveToday(schedule.TimeZoneId, utcNow);
        DateOnly yesterday = today.AddDays(-1);
        DateOnly effectiveFromDate = firstDate.Value > yesterday ? firstDate.Value : yesterday;
        if (lastDate.Value < effectiveFromDate)
        {
            return Array.Empty<ParkOpeningHoursCoverageSegment>();
        }

        DateOnly effectiveToDate = lastDate.Value;
        if (effectiveFromDate.DayNumber + MaximumCoverageDayCount - 1 < effectiveToDate.DayNumber)
        {
            effectiveToDate = DateOnly.FromDayNumber(effectiveFromDate.DayNumber + MaximumCoverageDayCount - 1);
        }

        List<ParkOpeningHoursCoverageSegment> segments = new List<ParkOpeningHoursCoverageSegment>();
        DateOnly? currentStart = null;
        DateOnly? currentEnd = null;

        for (DateOnly date = effectiveFromDate; date <= effectiveToDate; date = date.AddDays(1))
        {
            bool isDefined = IsDefined(schedule, date);
            if (isDefined)
            {
                currentStart ??= date;
                currentEnd = date;
                continue;
            }

            if (currentStart.HasValue && currentEnd.HasValue)
            {
                segments.Add(new ParkOpeningHoursCoverageSegment
                {
                    StartDate = currentStart.Value,
                    EndDate = currentEnd.Value,
                });
            }

            currentStart = null;
            currentEnd = null;
        }

        if (currentStart.HasValue && currentEnd.HasValue)
        {
            segments.Add(new ParkOpeningHoursCoverageSegment
            {
                StartDate = currentStart.Value,
                EndDate = currentEnd.Value,
            });
        }

        return segments;
    }

    private static bool IsDefined(ParkOpeningHoursSchedule schedule, DateOnly date)
    {
        if (schedule.DateOverrides.Any(dateOverride => dateOverride.LocalDate == date))
        {
            return true;
        }

        return schedule.RegularRules.Any(rule =>
            rule.StartDate <= date
            && rule.EndDate >= date
            && rule.DaysOfWeek.Contains(date.DayOfWeek));
    }

    private static DateOnly? ResolveFirstDate(ParkOpeningHoursSchedule schedule)
    {
        List<DateOnly> dates = new List<DateOnly>();
        dates.AddRange(schedule.RegularRules.Select(static rule => rule.StartDate));
        dates.AddRange(schedule.DateOverrides.Select(static dateOverride => dateOverride.LocalDate));
        return dates.Count == 0 ? null : dates.Min();
    }

    private static DateOnly? ResolveLastDate(ParkOpeningHoursSchedule schedule)
    {
        List<DateOnly> dates = new List<DateOnly>();
        dates.AddRange(schedule.RegularRules.Select(static rule => rule.EndDate));
        dates.AddRange(schedule.DateOverrides.Select(static dateOverride => dateOverride.LocalDate));
        return dates.Count == 0 ? null : dates.Max();
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
