namespace AmusementPark.Application.Features.Trips.Ports;

public interface ITripAuditReconciler
{
    Task<int> ReconcilePendingAsync(
        int maximumCount,
        CancellationToken cancellationToken);
}
