using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Passport.Services;

public sealed class PassportWatchlistExportSource : IPassportWatchlistExportSource
{
    private readonly IWatchlistExportStore store;
    private readonly UserCollectionTargetReader targetReader;

    public PassportWatchlistExportSource(
        IWatchlistExportStore store,
        UserCollectionTargetReader targetReader)
    {
        this.store = store ?? throw new ArgumentNullException(nameof(store));
        this.targetReader = targetReader ?? throw new ArgumentNullException(nameof(targetReader));
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
        Task<IReadOnlyDictionary<string, UserCollectionTargetSnapshot>> parksTask =
            this.targetReader.ResolveAsync(
                CollectionTargetType.Park,
                parkIds,
                cancellationToken);
        Task<IReadOnlyDictionary<string, UserCollectionTargetSnapshot>> parkItemsTask =
            this.targetReader.ResolveAsync(
                CollectionTargetType.ParkItem,
                parkItemIds,
                cancellationToken);
        FactualChangeEventId[] eventIds = stored.Notifications
            .Select(static notification => notification.FactualEventId)
            .Concat(stored.Digests.SelectMany(static digest =>
                digest.Entries.Select(static entry => entry.FactualEventId)))
            .Distinct()
            .ToArray();
        Task<IReadOnlyCollection<FactualChangeEvent>> eventsTask =
            this.store.LoadFactualEventsAsync(eventIds, sourceBudget, cancellationToken);
        await Task.WhenAll(parksTask, parkItemsTask, eventsTask);

        IReadOnlyCollection<FactualChangeEvent> events = await eventsTask;
        return new PassportWatchlistExportData(
            stored.CollectionEntries,
            stored.Subscriptions,
            stored.Notifications,
            stored.Digests,
            stored.EmailPreference,
            stored.DeliveryAttempts,
            MapTargets(await parksTask),
            MapTargets(await parkItemsTask),
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

    private static IReadOnlyDictionary<string, PassportWatchlistTargetSnapshot> MapTargets(
        IReadOnlyDictionary<string, UserCollectionTargetSnapshot> targets)
    {
        return targets.ToDictionary(
            static pair => pair.Key,
            static pair => new PassportWatchlistTargetSnapshot(
                pair.Value.TargetType,
                pair.Value.Status,
                pair.Value.Name,
                pair.Value.ParentParkName),
            StringComparer.Ordinal);
    }
}
