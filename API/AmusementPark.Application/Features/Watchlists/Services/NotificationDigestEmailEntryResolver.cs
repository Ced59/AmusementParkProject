using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class NotificationDigestEmailEntryResolver
{
    private readonly IWatchSubscriptionRepository subscriptionRepository;
    private readonly IFactualChangeEventRepository eventRepository;
    private readonly UserCollectionTargetReader targetReader;

    public NotificationDigestEmailEntryResolver(
        IWatchSubscriptionRepository subscriptionRepository,
        IFactualChangeEventRepository eventRepository,
        UserCollectionTargetReader targetReader)
    {
        this.subscriptionRepository = subscriptionRepository
            ?? throw new ArgumentNullException(nameof(subscriptionRepository));
        this.eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
        this.targetReader = targetReader ?? throw new ArgumentNullException(nameof(targetReader));
    }

    public async Task<NotificationDigestEmailEntry[]> ResolveEligibleAsync(
        NotificationDigest digest,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(digest);
        WatchSubscriptionId[] subscriptionIds = digest.Entries
            .Select(static entry => entry.SubscriptionId)
            .Distinct()
            .ToArray();
        IReadOnlyCollection<WatchSubscription> subscriptions =
            await this.subscriptionRepository.ListOwnedByIdsAsync(
                digest.UserId,
                subscriptionIds,
                cancellationToken);
        Dictionary<WatchSubscriptionId, WatchSubscription> subscriptionsById = subscriptions
            .ToDictionary(static subscription => subscription.Id);
        NotificationDigestEntry[] eligibleDigestEntries = digest.Entries
            .Where(entry => subscriptionsById.TryGetValue(
                    entry.SubscriptionId,
                    out WatchSubscription? subscription)
                && !subscription.IsPaused
                && subscription.Frequency == digest.Frequency
                && subscription.Channels.Contains(NotificationChannel.Email)
                && subscription.EventTypes.Contains(entry.EventType))
            .ToArray();
        IReadOnlyCollection<FactualChangeEvent> factualEvents = await this.eventRepository.GetManyAsync(
            eligibleDigestEntries.Select(static entry => entry.FactualEventId).ToArray(),
            cancellationToken);
        Dictionary<FactualChangeEventId, FactualChangeEvent> eventsById = factualEvents
            .ToDictionary(static factualEvent => factualEvent.Id);
        NotificationDigestEntry[] currentEntries = eligibleDigestEntries
            .Where(entry => eventsById.TryGetValue(entry.FactualEventId, out FactualChangeEvent? factualEvent)
                && factualEvent.Revision == entry.SourceRevision
                && factualEvent.Type == entry.EventType
                && factualEvent.Status == entry.SourceStatus
                && factualEvent.VerifiedAtUtc.HasValue)
            .ToArray();
        IReadOnlyDictionary<CollectionTargetType, IReadOnlyDictionary<string, UserCollectionTargetSnapshot>> targets =
            await this.ResolveTargetsAsync(currentEntries, cancellationToken);
        return currentEntries
            .Select(entry =>
            {
                FactualChangeEvent factualEvent = eventsById[entry.FactualEventId];
                UserCollectionTargetSnapshot? target = FindTarget(targets, factualEvent);
                return new NotificationDigestEmailEntry(
                    target?.Name,
                    target?.ParentParkName,
                    factualEvent.Type,
                    factualEvent.Status,
                    factualEvent.Source.PublisherName,
                    factualEvent.VerifiedAtUtc!.Value);
            })
            .ToArray();
    }

    private async Task<IReadOnlyDictionary<CollectionTargetType,
        IReadOnlyDictionary<string, UserCollectionTargetSnapshot>>> ResolveTargetsAsync(
        IReadOnlyCollection<NotificationDigestEntry> entries,
        CancellationToken cancellationToken)
    {
        Dictionary<CollectionTargetType, IReadOnlyDictionary<string, UserCollectionTargetSnapshot>> result = new();
        foreach (IGrouping<FactualTargetType, NotificationDigestEntry> group in entries.GroupBy(
            static entry => entry.TargetType))
        {
            CollectionTargetType targetType = group.Key == FactualTargetType.Park
                ? CollectionTargetType.Park
                : CollectionTargetType.ParkItem;
            result[targetType] = await this.targetReader.ResolveAsync(
                targetType,
                group.Select(static entry => entry.TargetId).ToArray(),
                cancellationToken);
        }

        return result;
    }

    private static UserCollectionTargetSnapshot? FindTarget(
        IReadOnlyDictionary<CollectionTargetType,
            IReadOnlyDictionary<string, UserCollectionTargetSnapshot>> targets,
        FactualChangeEvent factualEvent)
    {
        CollectionTargetType targetType = factualEvent.Target.Type == FactualTargetType.Park
            ? CollectionTargetType.Park
            : CollectionTargetType.ParkItem;
        return targets.TryGetValue(targetType, out IReadOnlyDictionary<string, UserCollectionTargetSnapshot>? byId)
            && byId.TryGetValue(factualEvent.Target.TargetId, out UserCollectionTargetSnapshot? target)
                ? target
                : null;
    }
}
