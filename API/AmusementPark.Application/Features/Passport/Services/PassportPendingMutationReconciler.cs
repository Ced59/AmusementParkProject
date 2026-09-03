using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

/// <summary>
/// Reprend les opérations idempotentes interrompues sous le même verrou distribué
/// que les mutations interactives du contenu d'une visite.
/// </summary>
public sealed class PassportPendingMutationReconciler : IPassportPendingMutationReconciler
{
    public const int MaximumBatchSize = 50;

    private readonly IUserVisitRepository visitRepository;
    private readonly IRideOccurrenceRepository occurrenceRepository;
    private readonly IVisitContentMutationLeaseManager contentMutationLeaseManager;
    private readonly IPassportClock clock;

    public PassportPendingMutationReconciler(
        IUserVisitRepository visitRepository,
        IRideOccurrenceRepository occurrenceRepository,
        IVisitContentMutationLeaseManager contentMutationLeaseManager,
        IPassportClock clock)
    {
        ArgumentNullException.ThrowIfNull(visitRepository);
        ArgumentNullException.ThrowIfNull(occurrenceRepository);
        ArgumentNullException.ThrowIfNull(contentMutationLeaseManager);
        ArgumentNullException.ThrowIfNull(clock);
        this.visitRepository = visitRepository;
        this.occurrenceRepository = occurrenceRepository;
        this.contentMutationLeaseManager = contentMutationLeaseManager;
        this.clock = clock;
    }

    public async Task<int> ReconcileBatchAsync(
        int maximumOperationCount,
        CancellationToken cancellationToken)
    {
        if (maximumOperationCount is < 1 or > MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumOperationCount));
        }

        IReadOnlyCollection<PendingPassportMutationVisit> candidates =
            await this.occurrenceRepository.ListPendingAuditMutationVisitsAsync(
                maximumOperationCount,
                cancellationToken);
        int reconciledCount = 0;
        foreach (PendingPassportMutationVisit candidate in candidates)
        {
            Visit? visit = await this.visitRepository.GetOwnedAsync(
                candidate.VisitId,
                candidate.UserId,
                cancellationToken);
            if (visit is null || visit.Status != VisitStatus.Draft)
            {
                if (await this.occurrenceRepository.TryRejectPendingMutationAsync(
                    candidate,
                    this.clock.UtcNow,
                    cancellationToken))
                {
                    reconciledCount++;
                }

                continue;
            }

            IVisitContentMutationLease? contentMutationLease =
                await this.contentMutationLeaseManager.TryAcquireAsync(
                    visit,
                    this.clock.UtcNow,
                    cancellationToken);
            if (contentMutationLease is null)
            {
                continue;
            }

            await using (contentMutationLease)
            {
                using CancellationTokenSource? leaseCancellationSource =
                    PassportLeaseCancellation.Link(
                        contentMutationLease,
                        cancellationToken);
                CancellationToken guardedCancellationToken =
                    leaseCancellationSource?.Token ?? cancellationToken;
                bool canRecover = CanRecover(candidate, visit);
                bool reconciled = canRecover
                    ? await this.occurrenceRepository.TryCompletePendingMutationAsync(
                        candidate,
                        guardedCancellationToken)
                    : await this.occurrenceRepository.TryRejectPendingMutationAsync(
                        candidate,
                        this.clock.UtcNow,
                        guardedCancellationToken);
                if (reconciled)
                {
                    reconciledCount++;
                }
            }
        }

        return reconciledCount;
    }

    public async Task<bool> ReconcileBeforeLifecycleTransitionAsync(
        Visit visit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(visit);
        if (visit.Status != VisitStatus.Draft)
        {
            return false;
        }

        IVisitContentMutationLease? contentMutationLease =
            await this.contentMutationLeaseManager.TryAcquireAsync(
                visit,
                this.clock.UtcNow,
                cancellationToken);
        if (contentMutationLease is null)
        {
            return false;
        }

        await using (contentMutationLease)
        {
            using CancellationTokenSource? leaseCancellationSource =
                PassportLeaseCancellation.Link(
                    contentMutationLease,
                    cancellationToken);
            CancellationToken guardedCancellationToken =
                leaseCancellationSource?.Token ?? cancellationToken;
            while (true)
            {
                PendingPassportMutationVisit? candidate =
                    await this.occurrenceRepository.GetPendingMutationAsync(
                        visit.UserId,
                        visit.Id,
                        guardedCancellationToken);
                if (candidate is null)
                {
                    return true;
                }

                bool reconciled = CanRecover(candidate, visit)
                    ? await this.occurrenceRepository.TryCompletePendingMutationAsync(
                        candidate,
                        guardedCancellationToken)
                    : await this.occurrenceRepository.TryRejectPendingMutationAsync(
                        candidate,
                        this.clock.UtcNow,
                        guardedCancellationToken);
                if (!reconciled)
                {
                    return false;
                }
            }
        }
    }

    private static bool CanRecover(
        PendingPassportMutationVisit candidate,
        Visit visit)
    {
        return candidate.Kind switch
        {
            PendingPassportMutationKind.Creation =>
                candidate.CreationPreparation is not null
                && RideOccurrenceCreationPreparationVisitGuard.Matches(
                    candidate.CreationPreparation,
                    visit),
            PendingPassportMutationKind.Reorder => true,
            PendingPassportMutationKind.Delete => true,
            _ => false,
        };
    }
}
