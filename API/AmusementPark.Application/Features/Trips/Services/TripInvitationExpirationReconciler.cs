using AmusementPark.Application.Features.Trips.Ports;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripInvitationExpirationReconciler
{
    private readonly ITripInvitationRepository repository;

    public TripInvitationExpirationReconciler(ITripInvitationRepository repository)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public Task<int> ReconcileAsync(int limit, CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        return this.repository.ExpireElapsedAsync(limit, cancellationToken);
    }
}
