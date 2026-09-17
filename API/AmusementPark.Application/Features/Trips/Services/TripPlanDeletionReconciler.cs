using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripPlanDeletionReconciler
{
    private readonly ITripPlanRepository repository;

    public TripPlanDeletionReconciler(ITripPlanRepository repository)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<int> ReconcileAsync(int limit, CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        IReadOnlyCollection<TripPlan> pending = await this.repository.ListPendingDeletionAsync(
            limit,
            cancellationToken);
        int completed = 0;
        foreach (TripPlan trip in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await this.repository.PurgeChildrenAsync(trip.Id, cancellationToken);
            TripPlanWriteResult result = await this.repository.FinalizeDeletionOwnedAsync(
                trip,
                cancellationToken);
            if (result.Outcome == TripPlanWriteOutcome.Success)
            {
                completed++;
            }
        }

        return completed;
    }
}
