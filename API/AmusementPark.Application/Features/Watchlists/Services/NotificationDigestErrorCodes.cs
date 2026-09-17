namespace AmusementPark.Application.Features.Watchlists.Services;

public static class NotificationDigestErrorCodes
{
    public const string InvalidPayload = "notification-digest.invalid-payload";

    public const string TooManyNotifications = "notification-digest.too-many-notifications";

    public const string EventMissing = "notification-digest.event-missing";

    public const string InvalidSnapshot = "notification-digest.invalid-snapshot";
}
