namespace AmusementPark.Application.Features.Watchlists.Ports;

public interface IWatchlistAccountDeletionFence
{
    Task BlockAsync(string userId, CancellationToken cancellationToken);

    Task<bool> IsBlockedAsync(string userId, CancellationToken cancellationToken);

    Task<IReadOnlySet<string>> ListBlockedAsync(
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken);

    Task<string?> TryAcquireDeliveryLeaseAsync(
        string userId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    Task ReleaseDeliveryLeaseAsync(
        string leaseId,
        CancellationToken cancellationToken);
}
