using AmusementPark.Application.Features.Trips.Ports;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripPreferenceCleanupReconciler
{
    private readonly ITripPreferenceRepository preferences;

    public TripPreferenceCleanupReconciler(ITripPreferenceRepository preferences)
    {
        this.preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
    }

    public Task<int> ReconcileAsync(int limit, CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        return this.preferences.ReconcileDepartureCleanupAsync(limit, cancellationToken);
    }
}
