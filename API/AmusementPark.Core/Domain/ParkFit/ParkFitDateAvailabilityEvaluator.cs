using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Résout l'ouverture d'un parc sans confondre absence de calendrier et fermeture.
/// </summary>
public sealed class ParkFitDateAvailabilityEvaluator
{
    private readonly ParkOpeningHoursCalendarBuilder calendarBuilder =
        new ParkOpeningHoursCalendarBuilder();

    public ParkFitDateAvailability Evaluate(
        ParkOpeningHoursSchedule? schedule,
        DateOnly evaluationDate)
    {
        if (schedule is null)
        {
            return new ParkFitDateAvailability(
                ParkFitDateAvailabilityState.Unknown,
                evaluationDate,
                ParkFitCalendarState.CalendarNotPublished);
        }

        ParkOpeningHoursCalendar calendar = this.calendarBuilder.BuildCalendar(
            schedule,
            evaluationDate,
            evaluationDate);
        ParkOpeningHoursDay? day = calendar.Days.SingleOrDefault();
        if (day is null || !day.IsDefined)
        {
            return new ParkFitDateAvailability(
                ParkFitDateAvailabilityState.Unknown,
                evaluationDate,
                ResolveMissingCalendarState(schedule, evaluationDate),
                timeZoneId: schedule.TimeZoneId,
                sourceUrl: schedule.SourceUrl,
                lastVerifiedAtUtc: schedule.LastVerifiedAtUtc);
        }

        ParkFitCalendarState calendarState = ResolveDefinedCalendarState(day);
        ParkFitDateAvailabilityState availabilityState = calendarState switch
        {
            ParkFitCalendarState.OpenConfirmed => ParkFitDateAvailabilityState.Available,
            ParkFitCalendarState.ClosedConfirmed => ParkFitDateAvailabilityState.Unavailable,
            ParkFitCalendarState.ExceptionalClosure => ParkFitDateAvailabilityState.Unavailable,
            _ => ParkFitDateAvailabilityState.Unknown,
        };

        return new ParkFitDateAvailability(
            availabilityState,
            evaluationDate,
            calendarState,
            day.TimeRanges,
            schedule.TimeZoneId,
            schedule.SourceUrl,
            schedule.LastVerifiedAtUtc);
    }

    private static ParkFitCalendarState ResolveMissingCalendarState(
        ParkOpeningHoursSchedule schedule,
        DateOnly evaluationDate)
    {
        List<DateOnly> firstDates = schedule.RegularRules
            .Select(static rule => rule.StartDate)
            .Concat(schedule.DateOverrides.Select(static dateOverride => dateOverride.LocalDate))
            .ToList();
        List<DateOnly> lastDates = schedule.RegularRules
            .Select(static rule => rule.EndDate)
            .Concat(schedule.DateOverrides.Select(static dateOverride => dateOverride.LocalDate))
            .ToList();
        if (firstDates.Count == 0 || lastDates.Count == 0)
        {
            return ParkFitCalendarState.CalendarNotPublished;
        }

        return evaluationDate < firstDates.Min() || evaluationDate > lastDates.Max()
            ? ParkFitCalendarState.CalendarNotPublished
            : ParkFitCalendarState.CalendarIncomplete;
    }

    private static ParkFitCalendarState ResolveDefinedCalendarState(ParkOpeningHoursDay day)
    {
        if (day.IsClosed)
        {
            if (!day.IsClosureExplicitlyDeclared)
            {
                return ParkFitCalendarState.OpeningHoursUnknown;
            }

            return string.Equals(day.SourceKind, "override", StringComparison.Ordinal)
                ? ParkFitCalendarState.ExceptionalClosure
                : ParkFitCalendarState.ClosedConfirmed;
        }

        return day.TimeRanges.Count == 0
            ? ParkFitCalendarState.OpeningHoursUnknown
            : ParkFitCalendarState.OpenConfirmed;
    }
}
