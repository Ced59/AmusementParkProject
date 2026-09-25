using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Ports;

public interface ITripAuditReader
{
    Task<IReadOnlyCollection<TripActivityEvent>> ListAsync(
        TripPlanId tripPlanId,
        long? beforeSequence,
        int limit,
        CancellationToken cancellationToken);

    Task<long> GetLatestSequenceAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TripActivityEvent>> ListImportantAfterAsync(
        TripPlanId tripPlanId,
        TripMemberId currentMemberId,
        long afterSequence,
        DateTime occurredAfterUtc,
        int limit,
        CancellationToken cancellationToken);
}
