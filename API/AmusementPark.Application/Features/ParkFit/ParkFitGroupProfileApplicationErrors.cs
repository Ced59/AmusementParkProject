using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.ParkFit;

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

    public static ApplicationError ProfileLimitReached()
    {
        return ApplicationError.RuleViolation(
            "park-fit.group-profile.limit-reached",
            $"At most {ParkFitGroupProfile.MaximumProfilesPerOwner} Park Fit group profiles are allowed per owner.");
    }

    public static ApplicationError ChangedConcurrently()
    {
        return ApplicationError.Conflict(
            "park-fit.group-profile.changed-concurrently",
            "The Park Fit group profile changed before this action completed.");
    }
}
