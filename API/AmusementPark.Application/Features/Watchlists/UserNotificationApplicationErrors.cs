using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Watchlists;

public static class UserNotificationApplicationErrors
{
    public static ApplicationError Invalid()
    {
        return ApplicationError.Validation(
            "notification.invalid-request",
            "The notification request is invalid.");
    }

    public static ApplicationError NotFound()
    {
        return ApplicationError.NotFound(
            "notification.not-found",
            "The notification was not found.");
    }

    public static ApplicationError ChangedConcurrently()
    {
        return ApplicationError.Conflict(
            "notification.changed-concurrently",
            "The notification changed before this action completed.");
    }
}
