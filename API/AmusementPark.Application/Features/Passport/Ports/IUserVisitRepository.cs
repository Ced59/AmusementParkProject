using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Ports;

/// <summary>
/// Persistance privée des visites, toujours bornée au propriétaire.
/// </summary>
public interface IUserVisitRepository
{
    Task<IdempotentVisitCreationResult?> ResolveExistingCreationAsync(
        Visit requestedVisit,
        string clientOperationId,
        CancellationToken cancellationToken);

    Task<IdempotentVisitCreationResult> CreateIdempotentAsync(
        Visit visit,
        string clientOperationId,
        CancellationToken cancellationToken);

    Task<IdempotentVisitCreationResult> CreateIdempotentAuditedAsync(
        Visit visit,
        string clientOperationId,
        PassportAuditEvent pendingAuditEvent,
        CancellationToken cancellationToken);

    Task<Visit?> GetOwnedAsync(
        VisitId visitId,
        string userId,
        CancellationToken cancellationToken);

    Task<UserVisitPage> ListOwnedAsync(
        UserVisitListCriteria criteria,
        CancellationToken cancellationToken);

    Task<bool> TryConfirmOwnedVersionAsync(
        VisitId visitId,
        string userId,
        long expectedVersion,
        CancellationToken cancellationToken);

    Task<bool> TryUpdateOwnedAsync(
        Visit visit,
        long expectedVersion,
        CancellationToken cancellationToken);

    Task<bool> TryUpdateOwnedAuditedAsync(
        Visit visit,
        long expectedVersion,
        PassportAuditEvent pendingAuditEvent,
        CancellationToken cancellationToken);

    Task<bool> TryDeleteOwnedAsync(
        VisitId visitId,
        string userId,
        long expectedVersion,
        CancellationToken cancellationToken);
}
