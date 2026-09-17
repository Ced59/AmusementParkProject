using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Ports;

public interface IUserNotificationRepository
{
    Task<UserNotificationCreationResult> CreateManyAsync(
        IReadOnlyCollection<UserNotification> notifications,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserNotification>> ListByFactualEventAsync(
        FactualChangeEventId eventId,
        string? afterNotificationId,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserNotification>> ListByFactualEventAndUsersAsync(
        FactualChangeEventId eventId,
        IReadOnlyCollection<string> userIds,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<UserNotification>> ListOwnedForDigestAsync(
        string userId,
        DateTime periodStartUtc,
        DateTime periodEndUtc,
        IReadOnlyCollection<NotificationDigestSubscriptionFilter> subscriptionFilters,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<FactualChangeEventId>> ListDeliveredFactualEventIdsOwnedAsync(
        string userId,
        IReadOnlyCollection<FactualChangeEventId> eventIds,
        CancellationToken cancellationToken);

    Task<long> RedeliverRetractionAsync(
        FactualChangeEventId eventId,
        IReadOnlyCollection<UserNotificationId> notificationIds,
        DateTime terminalAtUtc,
        DateTime deliveredAtUtc,
        CancellationToken cancellationToken);

    Task<PagedResult<UserNotification>> SearchOwnedAsync(
        string userId,
        UserNotificationSearchCriteria criteria,
        CancellationToken cancellationToken);

    Task<long> CountUnreadAsync(string userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<string>> ListParkIdsOwnedAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<UserNotification?> GetOwnedAsync(
        string userId,
        UserNotificationId notificationId,
        CancellationToken cancellationToken);

    Task<UserNotificationWriteOutcome> ReplaceAsync(
        UserNotification notification,
        long expectedVersion,
        CancellationToken cancellationToken);

    Task<long> MarkAllReadAsync(
        string userId,
        DateTime readAtUtc,
        CancellationToken cancellationToken);
}
