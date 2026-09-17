using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.Watchlists;

public static class NotificationEmailPreferenceApplicationErrors
{
    public static ApplicationError Invalid()
    {
        return ApplicationError.Validation(
            "notification-email-preference.invalid",
            "The notification email preference is invalid.");
    }

    public static ApplicationError EmailUnavailable()
    {
        return ApplicationError.RuleViolation(
            "notification-email-preference.email-unavailable",
            "A verified email address is required before email digests can be enabled.");
    }

    public static ApplicationError ConsentRequired()
    {
        return ApplicationError.RuleViolation(
            "notification-email-preference.consent-required",
            "Explicit consent is required before email digests can be enabled.");
    }

    public static ApplicationError ChangedConcurrently()
    {
        return ApplicationError.Conflict(
            "notification-email-preference.changed-concurrently",
            "The notification email preference changed before this action completed.");
    }
}
