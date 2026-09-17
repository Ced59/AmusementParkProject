using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Ports;

public interface IWatchSubscriptionRepository
{
    Task<IReadOnlyCollection<WatchSubscription>> ListOwnedAsync(
        string userId,
        CollectionTargetType? targetType,
        string? targetId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<WatchSubscription>> ListOwnedByIdsAsync(
        string userId,
        IReadOnlyCollection<WatchSubscriptionId> subscriptionIds,
        CancellationToken cancellationToken);

    Task<WatchSubscription?> GetOwnedAsync(
        string userId,
        WatchSubscriptionId subscriptionId,
        CancellationToken cancellationToken);

    Task<WatchSubscription?> GetOwnedByTargetAsync(
        string userId,
        CollectionTargetType targetType,
        string targetId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<WatchSubscription>> ListMatchingAsync(
        FactualChangeEvent factualEvent,
        string? afterSubscriptionId,
        int limit,
        CancellationToken cancellationToken);

    Task<WatchSubscriptionWriteOutcome> CreateAsync(
        WatchSubscription subscription,
        CancellationToken cancellationToken);

    Task<WatchSubscriptionWriteOutcome> ReplaceAsync(
        WatchSubscription subscription,
        long expectedVersion,
        CancellationToken cancellationToken);

    Task<WatchSubscriptionWriteOutcome> DeleteAsync(
        string userId,
        WatchSubscriptionId subscriptionId,
        long expectedVersion,
        CancellationToken cancellationToken);
}
