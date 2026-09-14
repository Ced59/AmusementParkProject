using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.ParkFit;

public static class ParkFitGroupProfileApplicationErrors
{
    public static ApplicationError Invalid(string code, string message)
    {
        return ApplicationError.Validation(code, message);
    }

    public static ApplicationError NotFound()
    {
        return ApplicationError.NotFound(
            "park-fit.group-profile.not-found",
            "The Park Fit group profile was not found.");
    }

    public static ApplicationError AliasConflict()
    {
        return ApplicationError.Conflict(
            "park-fit.group-profile.alias-conflict",
            "A Park Fit group profile already uses this alias.");
    }

    public static ApplicationError ChangedConcurrently()
    {
        return ApplicationError.Conflict(
            "park-fit.group-profile.changed-concurrently",
            "The Park Fit group profile changed before this action completed.");
    }
}
