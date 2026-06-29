using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.ParkOpeningHours;

public static class ParkOpeningHoursApplicationErrors
{
    public static ApplicationError ParkNotFound()
    {
        return ApplicationError.NotFound("park-opening-hours.park-not-found", "Le parc est introuvable.");
    }

    public static ApplicationError ScheduleNotFound()
    {
        return ApplicationError.NotFound("park-opening-hours.not-found", "Les horaires du parc sont introuvables.");
    }

    public static ApplicationError InvalidSchedule(IReadOnlyDictionary<string, IReadOnlyCollection<string>> details)
    {
        return ApplicationError.Validation("park-opening-hours.invalid", "Les horaires du parc sont invalides.", details);
    }
}
