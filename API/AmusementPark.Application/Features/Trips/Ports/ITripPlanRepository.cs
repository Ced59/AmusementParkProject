using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Ports;

public interface ITripPlanRepository
{
    Task<IdempotentTripPlanCreationResult?> ResolveExistingCreationAsync(
        TripPlan requestedTripPlan,
        string clientOperationId,
        CancellationToken cancellationToken);

    Task<IdempotentTripPlanCreationResult> CreateIdempotentAsync(
        TripPlan tripPlan,
        string clientOperationId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TripPlan>> ListAccessibleAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<TripPlan?> GetAccessibleAsync(
        string userId,
        TripPlanId tripPlanId,
        CancellationToken cancellationToken);

    Task<TripPlan?> GetOwnedAsync(
        string userId,
        TripPlanId tripPlanId,
        CancellationToken cancellationToken);

    Task<TripPlanWriteOutcome> ReplaceOwnedAsync(
        TripPlan tripPlan,
        long expectedVersion,
        CancellationToken cancellationToken);

    Task<TripPlanWriteOutcome> DeleteOwnedAsync(
        string userId,
        TripPlanId tripPlanId,
        long expectedVersion,
        CancellationToken cancellationToken);
}
