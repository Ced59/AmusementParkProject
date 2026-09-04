using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

internal static class RideOccurrenceFencedPersistence
{
    public static RideOccurrenceCreationRequest Attach(
        RideOccurrenceCreationRequest request,
        IVisitContentMutationLease? lease)
    {
        return lease is null
            ? request
            : request with { ContentFenceToken = lease.ContentFenceToken };
    }

    public static RideOccurrenceReorderRequest Attach(
        RideOccurrenceReorderRequest request,
        IVisitContentMutationLease? lease)
    {
        return lease is null
            ? request
            : request with { ContentFenceToken = lease.ContentFenceToken };
    }

    public static Task<bool> TryConfirmAsync(
        IRideOccurrenceRepository repository,
        RideOccurrence occurrence,
        long expectedVersion,
        IVisitContentMutationLease? lease,
        CancellationToken cancellationToken)
    {
        return lease is null
            ? repository.TryConfirmOwnedVersionAsync(
                occurrence.Id,
                occurrence.VisitId,
                occurrence.UserId,
                expectedVersion,
                cancellationToken)
            : repository.TryConfirmOwnedVersionFencedAsync(
                occurrence.Id,
                occurrence.VisitId,
                occurrence.UserId,
                expectedVersion,
                lease.ContentFenceToken,
                cancellationToken);
    }

    public static Task<bool> TryUpdateAsync(
        IRideOccurrenceRepository repository,
        RideOccurrence occurrence,
        long expectedVersion,
        PassportAuditEvent? auditEvent,
        IVisitContentMutationLease? lease,
        CancellationToken cancellationToken)
    {
        if (auditEvent is null)
        {
            return lease is null
                ? repository.TryUpdateOwnedAsync(
                    occurrence,
                    expectedVersion,
                    cancellationToken)
                : repository.TryUpdateOwnedFencedAsync(
                    occurrence,
                    expectedVersion,
                    lease.ContentFenceToken,
                    cancellationToken);
        }

        return lease is null
            ? repository.TryUpdateOwnedAuditedAsync(
                occurrence,
                expectedVersion,
                auditEvent,
                cancellationToken)
            : repository.TryUpdateOwnedAuditedFencedAsync(
                occurrence,
                expectedVersion,
                auditEvent,
                lease.ContentFenceToken,
                cancellationToken);
    }

    public static Task<bool> TryDeleteAsync(
        IRideOccurrenceRepository repository,
        RideOccurrence occurrence,
        long expectedVersion,
        PassportAuditEvent? auditEvent,
        IVisitContentMutationLease? lease,
        CancellationToken cancellationToken)
    {
        if (auditEvent is null)
        {
            return lease is null
                ? repository.TryDeleteOwnedAsync(
                    occurrence,
                    expectedVersion,
                    cancellationToken)
                : repository.TryDeleteOwnedFencedAsync(
                    occurrence,
                    expectedVersion,
                    lease.ContentFenceToken,
                    cancellationToken);
        }

        return lease is null
            ? repository.TryDeleteOwnedAuditedAsync(
                occurrence,
                expectedVersion,
                auditEvent,
                cancellationToken)
            : repository.TryDeleteOwnedAuditedFencedAsync(
                occurrence,
                expectedVersion,
                auditEvent,
                lease.ContentFenceToken,
                cancellationToken);
    }
}
