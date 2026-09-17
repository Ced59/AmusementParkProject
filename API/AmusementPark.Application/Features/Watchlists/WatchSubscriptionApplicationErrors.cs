using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists;

public static class WatchSubscriptionApplicationErrors
{
    public static ApplicationError Invalid(string code, string message)
    {
        return ApplicationError.Validation(code, message);
    }

    public static ApplicationError TargetNotFound()
    {
        return ApplicationError.NotFound(
            "watch-subscription.target-not-found",
            "The public watch target was not found.");
    }

    public static ApplicationError NotFound()
    {
        return ApplicationError.NotFound(
            "watch-subscription.not-found",
            "The watch subscription was not found.");
    }

    public static ApplicationError LimitReached()
    {
        return ApplicationError.RuleViolation(
            "watch-subscription.limit-reached",
            $"At most {WatchSubscription.MaximumSubscriptionsPerUser} watch subscriptions are allowed per user.");
    }

    public static ApplicationError ChangedConcurrently()
    {
        return ApplicationError.Conflict(
            "watch-subscription.changed-concurrently",
            "The watch subscription changed before this action completed.");
    }
}
