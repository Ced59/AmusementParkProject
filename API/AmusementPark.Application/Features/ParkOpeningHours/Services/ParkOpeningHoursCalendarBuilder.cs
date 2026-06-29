using AmusementPark.Application.Features.ParkOpeningHours.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOpeningHours.Services;

public sealed class ParkOpeningHoursCalendarBuilder
{
    private const int MaximumCalendarDayCount = 1096;

    public ParkOpeningHoursCalendarResult BuildCalendar(ParkOpeningHoursSchedule schedule, DateOnly? fromDate, DateOnly? toDate)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        DateOnly? firstDate = ResolveFirstDate(schedule);
        DateOnly? lastDate = ResolveLastDate(schedule);
        DateOnly effectiveFromDate = fromDate ?? firstDate ?? DateOnly.FromDateTime(DateTime.UtcNow.Date);
        DateOnly effectiveToDate = toDate ?? lastDate ?? effectiveFromDate;

        if (effectiveToDate < effectiveFromDate)
        {
            effectiveToDate = effectiveFromDate;
        }

        if (effectiveFromDate.DayNumber + MaximumCalendarDayCount - 1 < effectiveToDate.DayNumber)
        {
            effectiveToDate = DateOnly.FromDayNumber(effectiveFromDate.DayNumber + MaximumCalendarDayCount - 1);
        }

        List<ParkOpeningHoursDayResult> days = new List<ParkOpeningHoursDayResult>();
        for (DateOnly date = effectiveFromDate; date <= effectiveToDate; date = date.AddDays(1))
        {
            ParkOpeningHoursDayResult? day = ResolveDay(schedule, date);
            if (day is not null)
            {
                days.Add(day);
            }
        }

        return new ParkOpeningHoursCalendarResult
        {
            ParkId = schedule.ParkId,
            TimeZoneId = schedule.TimeZoneId,
            SourceUrl = schedule.SourceUrl,
            Notes = schedule.Notes,
            LastVerifiedAtUtc = schedule.LastVerifiedAtUtc,
            UpdatedAtUtc = schedule.UpdatedAtUtc,
            FirstDate = firstDate,
            LastDate = lastDate,
            FromDate = effectiveFromDate,
            ToDate = effectiveToDate,
            Days = days,
        };
    }

    private static ParkOpeningHoursDayResult? ResolveDay(ParkOpeningHoursSchedule schedule, DateOnly date)
    {
        ParkOpeningHoursDateOverride? dateOverride = schedule.DateOverrides.FirstOrDefault(dateOverride => dateOverride.LocalDate == date);
        if (dateOverride is not null)
        {
            return new ParkOpeningHoursDayResult
            {
                LocalDate = date,
                IsClosed = dateOverride.IsClosed || dateOverride.TimeRanges.Count == 0,
                IsDefined = true,
                SourceKind = "override",
                Label = dateOverride.Label,
                Reason = dateOverride.Reason,
                TimeRanges = dateOverride.TimeRanges.Select(static timeRange => timeRange.ToResult()).ToList(),
            };
        }

        ParkOpeningHoursRule? rule = schedule.RegularRules
            .Where(rule => rule.StartDate <= date && rule.EndDate >= date && rule.DaysOfWeek.Contains(date.DayOfWeek))
            .OrderBy(static rule => rule.SortOrder)
            .ThenByDescending(static rule => rule.StartDate)
            .FirstOrDefault();

        if (rule is null)
        {
            return null;
        }

        return new ParkOpeningHoursDayResult
        {
            LocalDate = date,
            IsClosed = rule.IsClosed || rule.TimeRanges.Count == 0,
            IsDefined = true,
            SourceKind = "regular",
            Label = rule.Label,
            Reason = rule.Reason,
            TimeRanges = rule.TimeRanges.Select(static timeRange => timeRange.ToResult()).ToList(),
        };
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
