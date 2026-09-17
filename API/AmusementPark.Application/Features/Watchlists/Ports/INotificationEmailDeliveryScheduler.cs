using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Ports;

public interface INotificationEmailDeliveryScheduler
{
    Task ScheduleAsync(NotificationDigest digest, CancellationToken cancellationToken);
}
