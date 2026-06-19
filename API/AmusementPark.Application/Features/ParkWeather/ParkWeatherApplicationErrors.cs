using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.ParkWeather;

internal static class ParkWeatherApplicationErrors
{
    public static ApplicationError ParkNotFound()
    {
        return ApplicationError.NotFound("park-weather.park-not-found", "Park not found or not visible.");
    }

    public static ApplicationError ParkHasNoCoordinates(string parkId)
    {
        return ApplicationError.RuleViolation("park-weather.park-no-coordinates", $"Park '{parkId}' has no valid coordinates.");
    }

    public static ApplicationError RunNotFound()
    {
        return ApplicationError.NotFound("park-weather.run-not-found", "Weather refresh run not found.");
    }

    public static ApplicationError ActiveRunExists()
    {
        return ApplicationError.Conflict("park-weather.run-active", "A weather refresh run is already active.");
    }

    public static ApplicationError InvalidRequest(string fieldName, string message)
    {
        return ApplicationError.Validation(
            "park-weather.invalid-request",
            message,
            new Dictionary<string, IReadOnlyCollection<string>>
            {
                [fieldName] = new[] { message },
            });
    }

    public static ApplicationError NoFailedParkToRetry()
    {
        return ApplicationError.RuleViolation("park-weather.no-failed-park", "No failed park is available for retry.");
    }
}
