using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips;

public static class TripPlanApplicationErrors
{
    public static ApplicationError Invalid(string code, string message)
    {
        return ApplicationError.Validation(code, message);
    }

    public static ApplicationError NotFound()
    {
        return ApplicationError.NotFound("trip.plan.not-found", "The trip plan was not found.");
    }

    public static ApplicationError LimitReached()
    {
        return ApplicationError.RuleViolation(
            "trip.plan.limit-reached",
            $"At most {TripPlan.MaximumPlansPerOwner} trip plans are allowed per owner.");
    }

    public static ApplicationError ChangedConcurrently(long? currentVersion)
    {
        return ApplicationError.Conflict(
            "trip.plan.changed-concurrently",
            "The trip plan changed before this action completed.",
            currentVersion);
    }

    public static ApplicationError IdempotencyConflict()
    {
        return ApplicationError.Conflict(
            "trip.plan.idempotency-conflict",
            "The idempotency key was already used with different trip details.");
    }

    public static ApplicationError CreationWasDeleted()
    {
        return ApplicationError.Conflict(
            "trip.plan.creation-deleted",
            "The trip created by this idempotency key was deleted and cannot be recreated by a retry.");
    }

    public static ApplicationError RecentAuthenticationRequired()
    {
        return ApplicationError.Forbidden(
            "trip.plan.recent-authentication-required",
            "Deleting a trip requires a recent authentication confirmation.");
    }
}
