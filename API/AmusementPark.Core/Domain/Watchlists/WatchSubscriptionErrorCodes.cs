namespace AmusementPark.Core.Domain.Watchlists;

public static class WatchSubscriptionErrorCodes
{
    public const string InvalidState = "watch.subscription.invalid-state";
    public const string InvalidTargetType = "watch.subscription.invalid-target-type";
    public const string EmptyEventTypes = "watch.subscription.empty-event-types";
    public const string InvalidEventType = "watch.subscription.invalid-event-type";
    public const string IncompatibleEventType = "watch.subscription.incompatible-event-type";
    public const string InvalidFrequency = "watch.subscription.invalid-frequency";
    public const string InvalidChannel = "watch.subscription.invalid-channel";
    public const string IncompatibleDeliveryPreference =
        "watch.subscription.incompatible-delivery-preference";
    public const string InvalidVersion = "watch.subscription.invalid-version";
    public const string InvalidTimestamp = "watch.subscription.invalid-timestamp";
}
