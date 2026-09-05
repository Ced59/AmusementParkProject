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

    public static ApplicationError InvalidOfficialMapFile()
    {
        return ApplicationError.Validation(
            "park.official-map.file-invalid",
            "An official map file between 1 byte and 25 MB and a stable map identifier are required.");
    }

    public static ApplicationError UnsupportedOfficialMapFile()
    {
        return ApplicationError.Validation(
            "park.official-map.file-unsupported",
            "Supported official map files are PDF, JPEG, PNG, WebP, GIF, KML, KMZ and ZIP.");
    }

    public static ApplicationError OfficialMapFileStorageFailed()
    {
        return ApplicationError.Technical(
            "park.official-map.file-storage-failed",
            "The official map file could not be stored.");
    }

    public static ApplicationError OfficialMapFileNotFound()
    {
        return ApplicationError.NotFound(
            "park.official-map.file-not-found",
            "The requested official map file was not found.");
    }
}
