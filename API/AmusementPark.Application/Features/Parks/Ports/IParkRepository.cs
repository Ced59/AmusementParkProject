using AmusementPark.Application.Common.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Ports;

/// <summary>
/// Port applicatif de persistance des parcs.
/// </summary>
public interface IParkRepository
{
    /// <summary>
    /// Retourne un parc par identifiant.
    /// </summary>
    Task<Park?> GetByIdAsync(string parkId, bool includeHidden, CancellationToken cancellationToken);

    /// <summary>
    /// Retourne une page de parcs.
    /// </summary>
    Task<PagedResult<Park>> GetPageAsync(int page, int pageSize, bool includeHidden, CancellationToken cancellationToken);

    /// <summary>
    /// Recherche des parcs par nom.
    /// </summary>
    Task<PagedResult<Park>> SearchByNameAsync(string name, int page, int pageSize, bool includeHidden, CancellationToken cancellationToken);

    /// <summary>
    /// Recherche des parcs par position.
    /// </summary>
    Task<IReadOnlyCollection<Park>> SearchByLocationAsync(double latitude, double longitude, double radiusInKilometers, bool includeHidden, CancellationToken cancellationToken);

    /// <summary>
    /// Crée un parc.
    /// </summary>
    Task<Park> CreateAsync(Park park, CancellationToken cancellationToken);

    /// <summary>
    /// Met à jour un parc.
    /// </summary>
    Task<Park?> UpdateAsync(string parkId, Park park, CancellationToken cancellationToken);

    /// <summary>
    /// Met à jour uniquement la visibilité d'un parc.
    /// </summary>
    Task<Park?> UpdateVisibilityAsync(string parkId, bool isVisible, CancellationToken cancellationToken);
}
