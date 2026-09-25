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

    public static ApplicationError CandidateNotFound()
    {
        return ApplicationError.NotFound(
            "trip.candidate.not-found",
            "The park candidate was not found.");
    }

    public static ApplicationError ParkNotAvailable()
    {
        return ApplicationError.NotFound(
            "trip.park.not-available",
            "The park is not available for trip planning.");
    }

    public static ApplicationError CandidateAlreadyExists()
    {
        return ApplicationError.Conflict(
            "trip.candidate.already-exists",
            "This park is already a candidate for the trip.");
    }

    public static ApplicationError CandidateIdempotencyConflict()
    {
        return ApplicationError.Conflict(
            "trip.candidate.idempotency-conflict",
            "The idempotency key was already used with different candidate details.");
    }

    public static ApplicationError CandidateCreationWasDeleted()
    {
        return ApplicationError.Conflict(
            "trip.candidate.creation-deleted",
            "The candidate created by this idempotency key was deleted and cannot be recreated by a retry.");
    }

    public static ApplicationError ChildMutationUnavailable()
    {
        return ApplicationError.Conflict(
            "trip.program.mutation-unavailable",
            "The trip changed or another program action is still completing.");
    }

    public static ApplicationError ExportUnavailable()
    {
        return ApplicationError.Conflict(
            "trip.export.unavailable",
            "The portable trip plan could not be prepared safely.");
    }

    public static ApplicationError CandidateIsUsedByDay()
    {
        return ApplicationError.RuleViolation(
            "trip.candidate.used-by-day",
            "A park assigned to a decided day cannot be removed.");
    }

    public static ApplicationError CandidateChangeInvalidatesDay()
    {
        return ApplicationError.RuleViolation(
            "trip.candidate.change-invalidates-day",
            "A park assigned to a decided day must stay selected and available on that date.");
    }

    public static ApplicationError DayNotFound()
    {
        return ApplicationError.NotFound(
            "trip.day.not-found",
            "The trip day was not found.");
    }

    public static ApplicationError PreferenceItemNotAvailable()
    {
        return ApplicationError.NotFound(
            "trip.preference.item-not-available",
            "The attraction is not available among the trip's candidate parks.");
    }

    public static ApplicationError PreferenceChangedConcurrently(long? currentVersion)
    {
        return ApplicationError.Conflict(
            "trip.preference.changed-concurrently",
            "The preference changed before this action completed.",
            currentVersion);
    }

    public static ApplicationError DecisionForbidden()
    {
        return ApplicationError.Forbidden(
            "trip.decision.forbidden",
            "Only the trip owner or an editor can record a group decision.");
    }

    public static ApplicationError DecisionChangedConcurrently(long? currentVersion)
    {
        return ApplicationError.Conflict(
            "trip.decision.changed-concurrently",
            "The group decision changed before this action completed.",
            currentVersion);
    }
}
