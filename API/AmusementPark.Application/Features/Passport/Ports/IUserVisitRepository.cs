using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Ports;

/// <summary>
/// Persistance privée des visites, toujours bornée au propriétaire.
/// </summary>
public interface IUserVisitRepository
{
    Task<Visit> CreateAsync(Visit visit, CancellationToken cancellationToken);

    Task<Visit?> GetOwnedAsync(
        VisitId visitId,
        string userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Visit>> ListOwnedAsync(
        string userId,
        int limit,
        CancellationToken cancellationToken);

    Task<bool> TryUpdateOwnedAsync(
        Visit visit,
        long expectedVersion,
        CancellationToken cancellationToken);

    Task<bool> TryDeleteOwnedAsync(
        VisitId visitId,
        string userId,
        long expectedVersion,
        CancellationToken cancellationToken);
}
