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

    public static ApplicationError InvalidDistanceRequest(string fieldName, string message)
    {
        return ApplicationError.Validation(
            "park.distance.invalid-request",
            message,
            new Dictionary<string, IReadOnlyCollection<string>>
            {
                [fieldName] = new[] { message },
            });
    }

    public static ApplicationError ParkHasNoCoordinates(string parkId)
    {
        return ApplicationError.RuleViolation("park.distance.no-coordinates", $"Park '{parkId}' has no coordinates");
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
