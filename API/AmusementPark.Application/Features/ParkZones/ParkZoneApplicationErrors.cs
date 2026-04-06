using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.ParkZones;

/// <summary>
/// Erreurs applicatives dédiées à la feature ParkZones avec messages alignés sur le legacy.
/// </summary>
internal static class ParkZoneApplicationErrors
{
    public static ApplicationError ParkZoneNotExists()
    {
        return ApplicationError.NotFound("park-zone.not-found", "Park zone not exists");
    }

    public static ApplicationError ErrorCreatingParkZone()
    {
        return ApplicationError.Technical("park-zone.create.failed", "Error while creating park zone");
    }

    public static ApplicationError ErrorUpdatingParkZone()
    {
        return ApplicationError.Technical("park-zone.update.failed", "Error while updating park zone");
    }

    public static ApplicationError ErrorDeletingParkZone()
    {
        return ApplicationError.Technical("park-zone.delete.failed", "Error while deleting park zone");
    }
}
