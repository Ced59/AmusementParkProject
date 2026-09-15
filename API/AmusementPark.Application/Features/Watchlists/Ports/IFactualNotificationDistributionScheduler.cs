using AmusementPark.Application.Features.FactualEvents.Models;

namespace AmusementPark.Application.Features.Watchlists.Ports;

public interface IFactualNotificationDistributionScheduler
{
    Task ScheduleAsync(
        string eventId,
        string? afterSubscriptionId,
        CancellationToken cancellationToken);

    Task<PublishedFactualEventCursor?> ReconcileAsync(
        PublishedFactualEventCursor? after,
        CancellationToken cancellationToken);
}
