using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists;

public static class LiveAlertApplicationErrors
{
    public static ApplicationError Invalid()
    {
        return ApplicationError.Validation("live-alert.invalid", "The live alert request is invalid.");
    }

    public static ApplicationError TargetUnavailable()
    {
        return ApplicationError.RuleViolation(
            "live-alert.target-unavailable",
            "A fresh public live observation is required before creating an alert.");
    }

    public static ApplicationError LimitReached()
    {
        return ApplicationError.RuleViolation(
            "live-alert.limit-reached",
            $"At most {LiveAlertSubscription.MaximumSubscriptionsPerUser} live alerts are allowed per user.");
    }

    public static ApplicationError NotFound()
    {
        return ApplicationError.NotFound("live-alert.not-found", "The live alert was not found.");
    }

    public static ApplicationError Conflict()
    {
        return ApplicationError.Conflict("live-alert.changed-concurrently", "The live alert changed before this action completed.");
    }
}
