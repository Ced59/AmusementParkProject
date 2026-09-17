using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Passport.Models;

public sealed record PassportWatchlistExportData(
    IReadOnlyCollection<UserCollectionEntry> CollectionEntries,
    IReadOnlyCollection<WatchSubscription> Subscriptions,
    IReadOnlyCollection<UserNotification> Notifications,
    IReadOnlyCollection<NotificationDigest> Digests,
    NotificationEmailPreference? EmailPreference,
    IReadOnlyCollection<NotificationDeliveryAttempt> DeliveryAttempts,
    IReadOnlyDictionary<string, PassportWatchlistTargetSnapshot> ParkTargets,
    IReadOnlyDictionary<string, PassportWatchlistTargetSnapshot> ParkItemTargets,
    IReadOnlyDictionary<FactualChangeEventId, FactualChangeEvent> FactualEvents)
{
    public static PassportWatchlistExportData Empty { get; } = new(
        Array.Empty<UserCollectionEntry>(),
        Array.Empty<WatchSubscription>(),
        Array.Empty<UserNotification>(),
        Array.Empty<NotificationDigest>(),
        null,
        Array.Empty<NotificationDeliveryAttempt>(),
        new Dictionary<string, PassportWatchlistTargetSnapshot>(StringComparer.Ordinal),
        new Dictionary<string, PassportWatchlistTargetSnapshot>(StringComparer.Ordinal),
        new Dictionary<FactualChangeEventId, FactualChangeEvent>());
}
