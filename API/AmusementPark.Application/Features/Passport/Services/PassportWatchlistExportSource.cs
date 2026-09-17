using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Passport.Services;

public sealed class PassportWatchlistExportSource : IPassportWatchlistExportSource
{
    private readonly IWatchlistExportStore store;

    public PassportWatchlistExportSource(IWatchlistExportStore store)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
    }

    public async Task<PassportWatchlistExportData> LoadAsync(
        string userId,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken)
    {
        string normalizedUserId = IdentifierRules.NormalizeRequired(userId, nameof(userId));
        ArgumentNullException.ThrowIfNull(sourceBudget);
        PassportWatchlistStoredExportData stored = await this.store.LoadAsync(
            normalizedUserId,
            sourceBudget,
            cancellationToken);
        string[] parkIds = ListTargetIds(stored, CollectionTargetType.Park);
        string[] parkItemIds = ListTargetIds(stored, CollectionTargetType.ParkItem);
        PassportWatchlistTargetCatalog targets = await this.store.LoadTargetsAsync(
            parkIds,
            parkItemIds,
            sourceBudget,
            cancellationToken);
        FactualChangeEventId[] eventIds = stored.Notifications
            .Select(static notification => notification.FactualEventId)
            .Distinct()
            .ToArray();
        IReadOnlyCollection<FactualChangeEvent> events =
            await this.store.LoadFactualEventsAsync(
                eventIds,
                sourceBudget,
                cancellationToken);
        return new PassportWatchlistExportData(
            stored.CollectionEntries,
            stored.Subscriptions,
            stored.Notifications,
            stored.Digests,
            stored.EmailPreference,
            stored.DeliveryAttempts,
            targets.ParkTargets,
            targets.ParkItemTargets,
            events.ToDictionary(static factualEvent => factualEvent.Id));
    }

    private static string[] ListTargetIds(
        PassportWatchlistStoredExportData stored,
        CollectionTargetType targetType)
    {
        IEnumerable<(CollectionTargetType Type, string Id)> targets = stored.CollectionEntries
            .Select(static entry => (entry.TargetType, entry.TargetId))
            .Concat(stored.Subscriptions.Select(static subscription =>
                (subscription.TargetType, subscription.TargetId)))
            .Concat(stored.Notifications.Select(static notification =>
                (ToCollectionTargetType(notification.TargetType), notification.TargetId)))
            .Concat(stored.Digests.SelectMany(static digest => digest.Entries.Select(static entry =>
                (ToCollectionTargetType(entry.TargetType), entry.TargetId))));
        return targets
            .Where(target => target.Type == targetType)
            .Select(static target => target.Id)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static CollectionTargetType ToCollectionTargetType(FactualTargetType targetType)
    {
        return targetType == FactualTargetType.Park
            ? CollectionTargetType.Park
            : CollectionTargetType.ParkItem;
    }

}
