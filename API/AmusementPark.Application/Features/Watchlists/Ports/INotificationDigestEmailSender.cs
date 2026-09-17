using AmusementPark.Application.Features.Watchlists.Models;

namespace AmusementPark.Application.Features.Watchlists.Ports;

public interface INotificationDigestEmailSender
{
    Task SendAsync(
        NotificationDigestEmailMessage message,
        CancellationToken cancellationToken);
}
