using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripNotificationCleanupReconciler
{
    private readonly ITripPlanRepository plans;
    private readonly ITripNotificationSubscriptionRepository subscriptions;

    public TripNotificationCleanupReconciler(
        ITripPlanRepository plans,
        ITripNotificationSubscriptionRepository subscriptions)
    {
        this.plans = plans ?? throw new ArgumentNullException(nameof(plans));
        this.subscriptions = subscriptions
            ?? throw new ArgumentNullException(nameof(subscriptions));
    }

    public async Task<TripNotificationCleanupBatchResult> ReconcileAsync(
        string? afterId,
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        IReadOnlyCollection<TripNotificationSubscription> batch =
            await this.subscriptions.ListForCleanupAsync(afterId, limit, cancellationToken);
        int deleted = 0;
        foreach (TripNotificationSubscription subscription in batch)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TripPlan? accessible = await this.plans.GetAccessibleAsync(
                subscription.UserId,
                subscription.TripPlanId,
                cancellationToken);
            if (accessible is not null)
            {
                continue;
            }

            await this.subscriptions.DeleteForMemberAsync(
                subscription.TripPlanId,
                subscription.UserId,
                cancellationToken);
            deleted++;
        }

        string? nextCursor = batch.Count == limit
            ? batch.Last().Id
            : null;
        return new TripNotificationCleanupBatchResult(batch.Count, deleted, nextCursor);
    }
}
