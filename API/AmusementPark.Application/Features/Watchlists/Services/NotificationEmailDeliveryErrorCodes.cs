namespace AmusementPark.Application.Features.Watchlists.Services;

public static class NotificationEmailDeliveryErrorCodes
{
    public const string InvalidPayload = "notification-email-delivery.invalid-payload";

    public const string DigestMissing = "notification-email-delivery.digest-missing";

    public const string DigestNotClosed = "notification-email-delivery.digest-not-closed";

    public const string PreferenceDisabled = "notification-email-delivery.preference-disabled";

    public const string EmailUnavailable = "notification-email-delivery.email-unavailable";

    public const string NoEligibleEntries = "notification-email-delivery.no-eligible-entries";

    public const string PersistenceConflict = "notification-email-delivery.persistence-conflict";

    public const string ProviderUnavailable = "notification-email-delivery.provider-unavailable";

    public const string AmbiguousProviderAcceptance =
        "notification-email-delivery.ambiguous-provider-acceptance";
}
