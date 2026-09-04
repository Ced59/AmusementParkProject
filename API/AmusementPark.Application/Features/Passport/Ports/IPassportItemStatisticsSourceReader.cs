using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Ports;

/// <summary>
/// Lit la projection minimale des observations privées actives d'un utilisateur pour un élément.
/// </summary>
public interface IPassportItemStatisticsSourceReader
{
    Task<IReadOnlyCollection<PassportItemRideObservation>> ReadAsync(
        string userId,
        string parkItemId,
        CancellationToken cancellationToken);
}
