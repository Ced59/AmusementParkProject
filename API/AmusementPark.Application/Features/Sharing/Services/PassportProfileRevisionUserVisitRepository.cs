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
            ResolveSegments(visit),
            token => this.inner.CreateIdempotentAsync(visit, clientOperationId, token),
            static result => result.Status == IdempotentVisitCreationStatus.Created,
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
            ResolveSegments(visit),
            token => this.inner.CreateIdempotentAuditedAsync(
                visit,
                clientOperationId,
                pendingAuditEvent,
                token),
            static result => result.Status == IdempotentVisitCreationStatus.Created,
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
        IReadOnlyCollection<(string ParkId, int Year)> segments =
            await this.ResolveUpdateSegmentsAsync(
            visit,
            cancellationToken);
        return await this.ExecuteMutationAsync(
            visit.UserId,
            segments,
            token => this.inner.TryUpdateOwnedAsync(visit, expectedVersion, token),
            static updated => updated,
            cancellationToken);
    }

    public async Task<bool> TryUpdateOwnedAuditedAsync(
        Visit visit,
        long expectedVersion,
        PassportAuditEvent pendingAuditEvent,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<(string ParkId, int Year)> segments =
            await this.ResolveUpdateSegmentsAsync(
            visit,
            cancellationToken);
        return await this.ExecuteMutationAsync(
            visit.UserId,
            segments,
            token => this.inner.TryUpdateOwnedAuditedAsync(
                visit,
                expectedVersion,
                pendingAuditEvent,
                token),
            static updated => updated,
            cancellationToken);
    }

    public async Task<bool> TryUpdateOwnedAuditedWithinContentMutationLeaseAsync(
        Visit visit,
        long expectedVersion,
        PassportAuditEvent pendingAuditEvent,
        string contentMutationLeaseToken,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<(string ParkId, int Year)> segments =
            await this.ResolveUpdateSegmentsAsync(
            visit,
            cancellationToken);
        return await this.ExecuteMutationAsync(
            visit.UserId,
            segments,
            token => this.inner.TryUpdateOwnedAuditedWithinContentMutationLeaseAsync(
                visit,
                expectedVersion,
                pendingAuditEvent,
                contentMutationLeaseToken,
                token),
            static updated => updated,
            cancellationToken);
    }

    private async Task<IReadOnlyCollection<(string ParkId, int Year)>> ResolveUpdateSegmentsAsync(
        Visit visit,
        CancellationToken cancellationToken)
    {
        Visit? previous = await this.inner.GetOwnedAsync(
            visit.Id,
            visit.UserId,
            cancellationToken);
        return ResolveSegments(previous, visit);
    }

    private async Task<TResult> ExecuteMutationAsync<TResult>(
        string ownerUserId,
        IReadOnlyCollection<(string ParkId, int Year)> segments,
        Func<CancellationToken, Task<TResult>> mutation,
        Func<TResult, bool> sourceChanged,
        CancellationToken cancellationToken)
    {
        if (segments.Count == 0)
        {
            return await mutation(cancellationToken);
        }

        IReadOnlyCollection<ShareSourceMutationLease> mutationLeases =
            await this.revisionGuard.TryBeginMutationAsync(
                ownerUserId,
                segments,
                cancellationToken);
        CancellationToken[] leaseCancellationTokens = mutationLeases
            .Select(static lease => lease.LeaseCancellationToken)
            .Prepend(cancellationToken)
            .ToArray();
        using CancellationTokenSource? linkedCancellation = mutationLeases.Count == 0
            ? null
            : CancellationTokenSource.CreateLinkedTokenSource(leaseCancellationTokens);
        CancellationToken guardedCancellationToken =
            linkedCancellation?.Token ?? cancellationToken;
        try
        {
            TResult result = await mutation(guardedCancellationToken);
            await this.revisionGuard.CompleteMutationAsync(
                mutationLeases,
                sourceChanged(result),
                CancellationToken.None);
            return result;
        }
        catch
        {
            await this.revisionGuard.CompleteMutationAsync(
                mutationLeases,
                true,
                CancellationToken.None);
            throw;
        }
    }

    private static IReadOnlyCollection<(string ParkId, int Year)> ResolveSegments(
        params Visit?[] visits)
    {
        return visits
            .Where(static visit => visit is not null
                && PassportProfileSourceMutationPolicy.CanChangeCompletedVisitProjection(
                    visit.Status))
            .Select(static visit => (visit!.ParkId, visit.Date.Year))
            .Distinct()
            .ToArray();
    }
}
