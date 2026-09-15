namespace AmusementPark.Core.Domain.Watchlists;

public static class UserNotificationErrorCodes
{
    public const string EventNotDistributable = "notification.event-not-distributable";
    public const string SubscriptionDoesNotMatch = "notification.subscription-does-not-match";
    public const string InvalidLanguage = "notification.invalid-language";
    public const string InvalidStatus = "notification.invalid-status";
    public const string InvalidTimestamp = "notification.invalid-timestamp";
    public const string InvalidVersion = "notification.invalid-version";
    public const string InvalidTransition = "notification.invalid-transition";
}
