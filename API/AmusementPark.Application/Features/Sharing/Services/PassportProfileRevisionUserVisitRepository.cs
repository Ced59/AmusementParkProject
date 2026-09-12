using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class PassportProfileRevisionUserVisitRepository : IUserVisitRepository
{
    private readonly IUserVisitRepository inner;
    private readonly IPassportProfileShareSourceRevisionGuard revisionGuard;

    public PassportProfileRevisionUserVisitRepository(
        IUserVisitRepository inner,
        IPassportProfileShareSourceRevisionGuard revisionGuard)
    {
        this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        this.revisionGuard = revisionGuard ?? throw new ArgumentNullException(nameof(revisionGuard));
    }

    public Task<IdempotentVisitCreationResult?> ResolveExistingCreationAsync(
        Visit requestedVisit,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        return this.inner.ResolveExistingCreationAsync(
            requestedVisit,
            clientOperationId,
            cancellationToken);
    }

    public Task<IdempotentVisitCreationResult> CreateIdempotentAsync(
        Visit visit,
        string clientOperationId,
        CancellationToken cancellationToken)
    {
        return this.ExecuteMutationAsync(
            visit.UserId,
            token => this.inner.CreateIdempotentAsync(visit, clientOperationId, token),
            static result => result.Status == IdempotentVisitCreationStatus.Created,
            PassportProfileSourceMutationPolicy.CanChangeCompletedVisitProjection(visit.Status),
            cancellationToken);
    }

    public Task<IdempotentVisitCreationResult> CreateIdempotentAuditedAsync(
        Visit visit,
        string clientOperationId,
        PassportAuditEvent pendingAuditEvent,
        CancellationToken cancellationToken)
    {
        return this.ExecuteMutationAsync(
            visit.UserId,
            token => this.inner.CreateIdempotentAuditedAsync(
                visit,
                clientOperationId,
                pendingAuditEvent,
                token),
            static result => result.Status == IdempotentVisitCreationStatus.Created,
            PassportProfileSourceMutationPolicy.CanChangeCompletedVisitProjection(visit.Status),
            cancellationToken);
    }

    public Task<Visit?> GetOwnedAsync(
        VisitId visitId,
        string userId,
        CancellationToken cancellationToken)
    {
        return this.inner.GetOwnedAsync(visitId, userId, cancellationToken);
    }

    public Task<UserVisitPage> ListOwnedAsync(
        UserVisitListCriteria criteria,
        CancellationToken cancellationToken)
    {
        return this.inner.ListOwnedAsync(criteria, cancellationToken);
    }

    public Task<IReadOnlyCollection<Visit>> ListAllOwnedForExportAsync(
        string userId,
        PassportExportSourceBudget sourceBudget,
        CancellationToken cancellationToken)
    {
        return this.inner.ListAllOwnedForExportAsync(userId, sourceBudget, cancellationToken);
    }

    public Task<bool> TryConfirmOwnedVersionAsync(
        VisitId visitId,
        string userId,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        return this.inner.TryConfirmOwnedVersionAsync(
            visitId,
            userId,
            expectedVersion,
            cancellationToken);
    }

    public async Task<bool> TryUpdateOwnedAsync(
        Visit visit,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        bool canChangeSource = await this.CanUpdateCompletedProjectionAsync(
            visit,
            cancellationToken);
        return await this.ExecuteMutationAsync(
            visit.UserId,
            token => this.inner.TryUpdateOwnedAsync(visit, expectedVersion, token),
            static updated => updated,
            canChangeSource,
            cancellationToken);
    }

    public async Task<bool> TryUpdateOwnedAuditedAsync(
        Visit visit,
        long expectedVersion,
        PassportAuditEvent pendingAuditEvent,
        CancellationToken cancellationToken)
    {
        bool canChangeSource = await this.CanUpdateCompletedProjectionAsync(
            visit,
            cancellationToken);
        return await this.ExecuteMutationAsync(
            visit.UserId,
            token => this.inner.TryUpdateOwnedAuditedAsync(
                visit,
                expectedVersion,
                pendingAuditEvent,
                token),
            static updated => updated,
            canChangeSource,
            cancellationToken);
    }

    public async Task<bool> TryUpdateOwnedAuditedWithinContentMutationLeaseAsync(
        Visit visit,
        long expectedVersion,
        PassportAuditEvent pendingAuditEvent,
        string contentMutationLeaseToken,
        CancellationToken cancellationToken)
    {
        bool canChangeSource = await this.CanUpdateCompletedProjectionAsync(
            visit,
            cancellationToken);
        return await this.ExecuteMutationAsync(
            visit.UserId,
            token => this.inner.TryUpdateOwnedAuditedWithinContentMutationLeaseAsync(
                visit,
                expectedVersion,
                pendingAuditEvent,
                contentMutationLeaseToken,
                token),
            static updated => updated,
            canChangeSource,
            cancellationToken);
    }

    private async Task<bool> CanUpdateCompletedProjectionAsync(
        Visit visit,
        CancellationToken cancellationToken)
    {
        Visit? previous = await this.inner.GetOwnedAsync(
            visit.Id,
            visit.UserId,
            cancellationToken);
        return PassportProfileSourceMutationPolicy.CanChangeCompletedVisitProjection(
            previous?.Status,
            visit.Status);
    }

    private async Task<TResult> ExecuteMutationAsync<TResult>(
        string ownerUserId,
        Func<CancellationToken, Task<TResult>> mutation,
        Func<TResult, bool> sourceChanged,
        bool canChangeSource,
        CancellationToken cancellationToken)
    {
        if (!canChangeSource)
        {
            return await mutation(cancellationToken);
        }

        ShareSourceMutationLease? mutationLease =
            await this.revisionGuard.TryBeginMutationAsync(ownerUserId, cancellationToken);
        using CancellationTokenSource? linkedCancellation = mutationLease is null
            ? null
            : CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken,
                mutationLease.LeaseCancellationToken);
        CancellationToken guardedCancellationToken =
            linkedCancellation?.Token ?? cancellationToken;
        try
        {
            TResult result = await mutation(guardedCancellationToken);
            await this.revisionGuard.CompleteMutationAsync(
                mutationLease,
                sourceChanged(result),
                CancellationToken.None);
            return result;
        }
        catch
        {
            await this.revisionGuard.CompleteMutationAsync(
                mutationLease,
                true,
                CancellationToken.None);
            throw;
        }
    }
}
