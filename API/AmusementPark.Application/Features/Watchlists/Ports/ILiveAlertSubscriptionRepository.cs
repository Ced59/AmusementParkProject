using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Ports;

public interface ILiveAlertSubscriptionRepository
{
    Task<IReadOnlyCollection<LiveAlertSubscription>> ListOwnedAsync(
        string userId,
        string? targetId,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LiveAlertSubscription>> ListActiveMatchingAsync(
        IReadOnlyCollection<string> targetIds,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<LiveAlertSubscription>> ListPendingAsync(
        DateTime nowUtc,
        int limit,
        CancellationToken cancellationToken);

    Task<WatchSubscriptionWriteOutcome> CreateAsync(
        LiveAlertSubscription subscription,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task<WatchSubscriptionWriteOutcome> ReplaceAsync(
        LiveAlertSubscription subscription,
        long expectedVersion,
        CancellationToken cancellationToken);

    Task<WatchSubscriptionWriteOutcome> DeleteAsync(
        string userId,
        LiveAlertSubscriptionId subscriptionId,
        long expectedVersion,
        CancellationToken cancellationToken);
}
