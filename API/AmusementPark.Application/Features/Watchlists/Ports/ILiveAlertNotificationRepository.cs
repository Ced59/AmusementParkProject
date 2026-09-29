using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Ports;

public interface ILiveAlertNotificationRepository
{
    Task<IReadOnlyCollection<LiveAlertNotification>> ListOwnedAsync(
        string userId,
        DateTime nowUtc,
        int limit,
        CancellationToken cancellationToken);

    Task<int> CountUnreadAsync(string userId, DateTime nowUtc, CancellationToken cancellationToken);

    Task<LiveAlertNotification?> GetOwnedAsync(
        string userId,
        LiveAlertNotificationId notificationId,
        CancellationToken cancellationToken);

    Task<UserNotificationWriteOutcome> CreateAsync(
        LiveAlertNotification notification,
        CancellationToken cancellationToken);

    Task<UserNotificationWriteOutcome> ReplaceAsync(
        LiveAlertNotification notification,
        long expectedVersion,
        CancellationToken cancellationToken);
}
