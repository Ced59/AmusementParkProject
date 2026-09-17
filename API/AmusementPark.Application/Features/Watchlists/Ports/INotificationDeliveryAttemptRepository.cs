using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Ports;

public interface INotificationDeliveryAttemptRepository
{
    Task<NotificationDeliveryAttempt?> GetAsync(
        string attemptId,
        CancellationToken cancellationToken);

    Task<NotificationDeliveryAttemptWriteOutcome> CreateAsync(
        NotificationDeliveryAttempt attempt,
        CancellationToken cancellationToken);

    Task<NotificationDeliveryAttemptWriteOutcome> ReplaceAsync(
        NotificationDeliveryAttempt attempt,
        long expectedVersion,
        CancellationToken cancellationToken);

    Task<NotificationDeliveryMetricsResult> GetMetricsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);
}
