using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.ParkItems;

/// <summary>
/// Erreurs applicatives dédiées à la feature ParkItems avec messages alignés sur le legacy.
/// </summary>
internal static class ParkItemApplicationErrors
{
    public static ApplicationError ParkItemNotExists()
    {
        return ApplicationError.NotFound("park-item.not-found", "Park item not exists");
    }

    public static ApplicationError AttractionManufacturerNotExists()
    {
        return ApplicationError.NotFound("attraction-manufacturer.not-found", "Attraction manufacturer not exists");
    }

    public static ApplicationError ErrorCreatingParkItem()
    {
        return ApplicationError.Technical("park-item.create.failed", "Error while creating park item");
    }

    public static ApplicationError ErrorUpdatingParkItem()
    {
        return ApplicationError.Technical("park-item.update.failed", "Error while updating park item");
    }

    public static ApplicationError ErrorDeletingParkItem()
    {
        return ApplicationError.Technical("park-item.delete.failed", "Error while deleting park item");
    }
}
