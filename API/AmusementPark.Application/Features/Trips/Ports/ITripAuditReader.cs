using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Ports;

public interface ITripAuditReader
{
    Task<IReadOnlyCollection<TripActivityEvent>> ListAsync(
        TripPlanId tripPlanId,
        long? beforeSequence,
        int limit,
        CancellationToken cancellationToken);

    Task<TripNotificationBoundary> GetNotificationBoundaryAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TripActivityEvent>> ListImportantAfterAsync(
        TripPlanId tripPlanId,
        TripMemberId currentMemberId,
        long afterSequence,
        IReadOnlyCollection<string> excludedOperationKeys,
        int limit,
        CancellationToken cancellationToken);
}
