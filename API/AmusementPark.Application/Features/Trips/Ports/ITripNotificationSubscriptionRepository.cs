using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Ports;

public interface ITripNotificationSubscriptionRepository
{
    Task<TripNotificationSubscription?> GetAsync(
        TripPlanId tripPlanId,
        string userId,
        CancellationToken cancellationToken);

    Task<bool> CreateAsync(
        TripNotificationSubscription subscription,
        CancellationToken cancellationToken);

    Task<bool> ReplaceAsync(
        TripNotificationSubscription subscription,
        long expectedVersion,
        CancellationToken cancellationToken);

    Task DeleteForMemberAsync(
        TripPlanId tripPlanId,
        string userId,
        CancellationToken cancellationToken);

    Task DeleteForTripAsync(
        TripPlanId tripPlanId,
        CancellationToken cancellationToken);
}
