namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Niveau factuel du calendrier pour la date demandée, sans interpréter une absence.
/// </summary>
public enum ParkFitCalendarState
{
    OpenConfirmed,
    ClosedConfirmed,
    CalendarNotPublished,
    CalendarIncomplete,
    ExceptionalClosure,
    OpeningHoursUnknown,
}
