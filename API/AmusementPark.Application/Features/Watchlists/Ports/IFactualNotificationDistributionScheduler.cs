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

    Task ScheduleCorrectionAsync(
        string eventId,
        long eventVersion,
        string? afterNotificationId,
        CancellationToken cancellationToken);

    Task<TerminalFactualEventCursor?> ReconcileCorrectionsAsync(
        TerminalFactualEventCursor? after,
        CancellationToken cancellationToken);
}
