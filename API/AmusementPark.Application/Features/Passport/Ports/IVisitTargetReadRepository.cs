using AmusementPark.Application.Features.Passport.Models;

namespace AmusementPark.Application.Features.Passport.Ports;

/// <summary>
/// Lecture minimale des informations nécessaires aux règles du journal de visite.
/// </summary>
public interface IVisitTargetReadRepository
{
    Task<IReadOnlyCollection<VisitTarget>> GetByIdsAsync(
        IReadOnlyCollection<string> parkItemIds,
        CancellationToken cancellationToken);
}
