using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Ports;

public interface ITripPreferenceRepository
{
    Task<IReadOnlyCollection<TripPreferenceCount>> SummarizeAsync(
        TripPlanId tripPlanId,
        IReadOnlyCollection<string> activeUserIds,
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken);

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
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken);

    Task<TripItemPreferenceWriteResult> ReplaceAsync(
        TripItemPreference preference,
        long expectedVersion,
        TripChildMutationLease lease,
        TripActivityWrite? pendingActivity,
        CancellationToken cancellationToken);

    Task<bool> CompleteDepartureCleanupAsync(
        TripPlanId tripPlanId,
        string userId,
        CancellationToken cancellationToken);

    Task CompleteDepartureCleanupMarkerAsync(
        TripPlanId tripPlanId,
        string userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TripDepartureCleanup>> ListPendingDepartureCleanupAsync(
        int limit,
        CancellationToken cancellationToken);
}
