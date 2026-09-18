using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Ports;

public interface ITripPreferenceRepository
{
    Task<IReadOnlyCollection<TripItemPreference>> ListForUserAsync(
        TripPlanId tripPlanId,
        string userId,
        CancellationToken cancellationToken);

    Task<TripItemPreference?> GetAsync(
        TripPlanId tripPlanId,
        string userId,
        string parkItemId,
        CancellationToken cancellationToken);

    Task<TripItemPreferenceWriteResult> CreateAsync(
        TripItemPreference preference,
        TripChildMutationLease lease,
        CancellationToken cancellationToken);

    Task<TripItemPreferenceWriteResult> ReplaceAsync(
        TripItemPreference preference,
        long expectedVersion,
        TripChildMutationLease lease,
        CancellationToken cancellationToken);

    Task CompleteDepartureCleanupAsync(
        TripPlanId tripPlanId,
        string userId,
        CancellationToken cancellationToken);

    Task<int> ReconcileDepartureCleanupAsync(
        int limit,
        CancellationToken cancellationToken);
}
