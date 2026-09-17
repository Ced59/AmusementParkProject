using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record PassportWatchlistStoredExportData(
    IReadOnlyCollection<UserCollectionEntry> CollectionEntries,
    IReadOnlyCollection<WatchSubscription> Subscriptions,
    IReadOnlyCollection<UserNotification> Notifications,
    IReadOnlyCollection<NotificationDigest> Digests,
    NotificationEmailPreference? EmailPreference,
    IReadOnlyCollection<NotificationDeliveryAttempt> DeliveryAttempts)
{
    public static PassportWatchlistStoredExportData Empty { get; } = new(
        Array.Empty<UserCollectionEntry>(),
        Array.Empty<WatchSubscription>(),
        Array.Empty<UserNotification>(),
        Array.Empty<NotificationDigest>(),
        null,
        Array.Empty<NotificationDeliveryAttempt>());
}
