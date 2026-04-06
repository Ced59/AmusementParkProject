using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Parks;

/// <summary>
/// Erreurs applicatives dédiées à la feature Parks avec messages alignés sur le legacy.
/// </summary>
internal static class ParkApplicationErrors
{
    public static ApplicationError ParkNotExists()
    {
        return ApplicationError.NotFound("park.not-found", "Park not exists");
    }

    public static ApplicationError NoParkInThisLocation()
    {
        return ApplicationError.NotFound("park.geo-search.empty", "They are no park in this location");
    }

    public static ApplicationError ErrorCreatingPark()
    {
        return ApplicationError.Technical("park.create.failed", "Error while creating park");
    }

    public static ApplicationError ErrorUpdatingPark()
    {
        return ApplicationError.Technical("park.update.failed", "Error while updating park");
    }
}
