using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Ports;

public interface INotificationDigestRepository
{
    Task ReplaceSnapshotAsync(NotificationDigest digest, CancellationToken cancellationToken);

    Task DeleteAsync(NotificationDigestId digestId, CancellationToken cancellationToken);

    Task<NotificationDigest?> GetAsync(
        NotificationDigestId digestId,
        CancellationToken cancellationToken);
}
