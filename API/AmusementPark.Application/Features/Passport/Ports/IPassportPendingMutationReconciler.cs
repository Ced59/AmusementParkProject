using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Ports;

/// <summary>
/// Termine un lot borné de mutations du passeport dont l'état métier a pu être
/// écrit avant l'acquittement de leur opération idempotente.
/// </summary>
public interface IPassportPendingMutationReconciler
{
    Task<IVisitContentMutationLease?> TryAcquireReconciledLifecycleLeaseAsync(
        Visit visit,
        CancellationToken cancellationToken);

    Task<bool> ReconcileBeforeLifecycleTransitionAsync(
        Visit visit,
        CancellationToken cancellationToken);

    Task<int> ReconcileBatchAsync(
        int maximumOperationCount,
        CancellationToken cancellationToken);
}
