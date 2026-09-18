using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Services;

public sealed class TripAdmissionReconciler
{
    private const int BatchSize = 50;
    private readonly ITripAdmissionRepository repository;
    private readonly TripAdmissionService service;
    private readonly TimeProvider timeProvider;

    public TripAdmissionReconciler(
        ITripAdmissionRepository repository,
        TripAdmissionService service,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository;
        this.service = service;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<int> ReconcileBatchAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TripInvitation> invitations =
            await this.repository.ListPendingAcceptancesAsync(BatchSize, cancellationToken);
        int reconciled = 0;
        foreach (TripInvitation invitation in invitations)
        {
            if (await this.service.ReconcileAsync(invitation, cancellationToken))
            {
                reconciled++;
            }
        }

        IReadOnlyCollection<TripPlan> plans =
            await this.repository.ListPendingAdmissionFencesAsync(BatchSize, cancellationToken);
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        foreach (TripPlan plan in plans)
        {
            TripMemberAdmissionFence? fence = plan.MemberAdmissionFence;
            if (fence is null || fence.LeaseExpiresAtUtc > nowUtc)
            {
                continue;
            }

            TripAdmissionWriteOutcome cancelled = await this.repository.CancelExpiredFenceAsync(
                plan.Id,
                fence,
                cancellationToken);
            if (cancelled == TripAdmissionWriteOutcome.Success)
            {
                await this.repository.CancelInvitationAcceptanceAsync(
                    fence.InvitationId,
                    fence,
                    cancellationToken);
                reconciled++;
            }
        }

        return reconciled;
    }
}
