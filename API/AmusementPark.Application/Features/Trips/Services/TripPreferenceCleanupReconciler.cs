using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripPreferenceCleanupReconciler
{
    private readonly ITripPreferenceRepository preferences;
    private readonly ITripNotificationSubscriptionRepository notifications;

    public TripPreferenceCleanupReconciler(
        ITripPreferenceRepository preferences,
        ITripNotificationSubscriptionRepository notifications)
    {
        this.preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
        this.notifications = notifications
            ?? throw new ArgumentNullException(nameof(notifications));
    }

    public async Task<int> ReconcileAsync(int limit, CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        IReadOnlyCollection<TripDepartureCleanup> pending =
            await this.preferences.ListPendingDepartureCleanupAsync(limit, cancellationToken);
        int completed = 0;
        foreach (TripDepartureCleanup cleanup in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!await this.preferences.CompleteDepartureCleanupAsync(
                    cleanup.TripPlanId,
                    cleanup.UserId,
                    cancellationToken))
            {
                continue;
            }

            await this.notifications.DeleteForMemberAsync(
                cleanup.TripPlanId,
                cleanup.UserId,
                cancellationToken);
            await this.preferences.CompleteDepartureCleanupMarkerAsync(
                cleanup.TripPlanId,
                cleanup.UserId,
                cancellationToken);
            completed++;
        }

        return completed;
    }
}
