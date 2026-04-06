using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkFounders.Ports;

/// <summary>
/// Port applicatif de persistance des park founders.
/// </summary>
public interface IParkFounderRepository
{
    /// <summary>
    /// Retourne tous les park founders.
    /// </summary>
    Task<IReadOnlyCollection<ParkFounder>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retourne un park founder par identifiant.
    /// </summary>
    Task<ParkFounder?> GetByIdAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Crée un park founder.
    /// </summary>
    Task<ParkFounder> CreateAsync(ParkFounder entity, CancellationToken cancellationToken);

    /// <summary>
    /// Met à jour un park founder existant.
    /// </summary>
    Task<ParkFounder?> UpdateAsync(string id, ParkFounder entity, CancellationToken cancellationToken);
}
