namespace AmusementPark.Core.Domain.Parks;

public sealed class ParkOpeningHoursCoverageSegmentBuilder
{
    private const int MaximumCoverageDayCount = 1096;

    public IReadOnlyCollection<ParkOpeningHoursCoverageSegment> BuildSegments(ParkOpeningHoursSchedule schedule)
    {
        return this.BuildSegments(schedule, DateTime.UtcNow);
    }

    public IReadOnlyCollection<ParkOpeningHoursCoverageSegment> BuildSegments(ParkOpeningHoursSchedule schedule, DateTime utcNow)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        DateOnly? firstDate = ResolveFirstDate(schedule);
        DateOnly? lastDate = ResolveLastDate(schedule);
        if (!firstDate.HasValue || !lastDate.HasValue)
        {
            return Array.Empty<ParkOpeningHoursCoverageSegment>();
        }

        DateOnly today = ParkOpeningHoursTimeZoneResolver.ResolveLocalDate(schedule.TimeZoneId, utcNow);
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
}
