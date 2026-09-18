using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips;

public static class TripInvitationApplicationErrors
{
    public static ApplicationError Invalid(string code, string message)
    {
        return ApplicationError.Validation(code, message);
    }

    public static ApplicationError NotFound()
    {
        return ApplicationError.NotFound(
            "trip.invitation.not-found",
            "The trip invitation was not found.");
    }

    public static ApplicationError ChangedConcurrently()
    {
        return ApplicationError.Conflict(
            "trip.invitation.changed-concurrently",
            "The trip invitation changed before this action completed.");
    }

    public static ApplicationError IdempotencyConflict()
    {
        return ApplicationError.Conflict(
            "trip.invitation.idempotency-conflict",
            "The idempotency key was already used with different invitation details.");
    }

    public static ApplicationError LimitReached()
    {
        return ApplicationError.RuleViolation(
            "trip.invitation.limit-reached",
            $"At most {TripInvitation.MaximumActiveInvitationsPerTrip} active invitations are allowed per trip.");
    }

    public static ApplicationError AdmissionsClosed()
    {
        return ApplicationError.RuleViolation(
            "trip.invitation.admissions-closed",
            "This trip no longer accepts invitations.");
    }

    public static ApplicationError CreationUnavailable()
    {
        return ApplicationError.Conflict(
            "trip.invitation.creation-unavailable",
            "The invitation could not be created safely; retry with the same idempotency key.");
    }

    public static ApplicationError ReplayUnavailable()
    {
        return ApplicationError.Conflict(
            "trip.invitation.replay-unavailable",
            "The original invitation link can no longer be replayed safely.");
    }
}
