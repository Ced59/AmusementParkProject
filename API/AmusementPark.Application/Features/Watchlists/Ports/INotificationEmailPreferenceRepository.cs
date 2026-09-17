using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Ports;

public interface INotificationEmailPreferenceRepository
{
    Task<NotificationEmailPreference?> GetAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<NotificationEmailPreferenceWriteOutcome> CreateAsync(
        NotificationEmailPreference preference,
        CancellationToken cancellationToken);

    Task<NotificationEmailPreferenceWriteOutcome> ReplaceAsync(
        NotificationEmailPreference preference,
        long expectedVersion,
        CancellationToken cancellationToken);
}
