namespace AmusementPark.Application.Features.Watchlists.Services;

public static class FactualNotificationDistributionErrorCodes
{
    public const string InvalidPayload = "watch-distribution.invalid-payload";
    public const string EventMissing = "watch-distribution.event-missing";
    public const string EventNotPublished = "watch-distribution.event-not-published";
    public const string InvalidNotification = "watch-distribution.invalid-notification";
    public const string EventNotTerminal = "watch-distribution.event-not-terminal";
    public const string SupersedingEventMissing = "watch-distribution.superseding-event-missing";
}
