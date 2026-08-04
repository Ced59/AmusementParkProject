using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Parks;

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

    public static ApplicationError ScheduleNotAllowed(ParkStatus status)
    {
        return ApplicationError.Validation(
            "park-opening-hours.not-operating",
            "Les horaires ne peuvent être configurés que pour un parc en activité.",
            new Dictionary<string, IReadOnlyCollection<string>>
            {
                ["parkStatus"] = new[] { $"Le statut '{status}' n'autorise pas les horaires actuels." },
            });
    }

    public static ApplicationError InvalidSchedule(IReadOnlyDictionary<string, IReadOnlyCollection<string>> details)
    {
        return ApplicationError.Validation("park-opening-hours.invalid", "Les horaires du parc sont invalides.", details);
    }
}
