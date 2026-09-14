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
                evaluationDate);
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
                evaluationDate);
        }

        return new ParkFitDateAvailability(
            day.IsClosed
                ? ParkFitDateAvailabilityState.Unavailable
                : ParkFitDateAvailabilityState.Available,
            evaluationDate);
    }
}
