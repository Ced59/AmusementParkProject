using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Ports;

public interface INotificationEmailDeliveryScheduler
{
    Task<string?> ScheduleAsync(NotificationDigest digest, CancellationToken cancellationToken);

    Task CancelAsync(string jobId, CancellationToken cancellationToken);
}
