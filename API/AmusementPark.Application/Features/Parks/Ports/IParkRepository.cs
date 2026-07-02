using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Features.Parks.Contracts;
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
    /// Retourne plusieurs parcs par identifiants.
    /// </summary>
    Task<IReadOnlyCollection<Park>> GetByIdsAsync(IEnumerable<string> parkIds, CancellationToken cancellationToken);

    /// <summary>
    /// Retourne une page de parcs.
    /// </summary>
    Task<PagedResult<Park>> GetPageAsync(int page, int pageSize, bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, bool? hasValidCoordinates, ClosedEntityFilter closedFilter, CancellationToken cancellationToken, ParkAdminSortField sortField = ParkAdminSortField.Default, bool sortDescending = false, ParkAudienceClassificationFilter? audienceClassificationFilter = null);

    /// <summary>
    /// Compte les parcs.
    /// </summary>
    Task<long> CountAsync(bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken);

    /// <summary>
    /// Retourne les identifiants des parcs visibles publiquement.
    /// </summary>
    Task<IReadOnlyCollection<string>> GetVisibleParkIdsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retourne les identifiants de parcs rattaches a un exploitant.
    /// </summary>
    Task<IReadOnlyCollection<string>> GetParkIdsByOperatorIdAsync(string operatorId, CancellationToken cancellationToken);

    /// <summary>
    /// Retourne les identifiants de parcs rattaches a un fondateur.
    /// </summary>
    Task<IReadOnlyCollection<string>> GetParkIdsByFounderIdAsync(string founderId, CancellationToken cancellationToken);

    /// <summary>
    /// Retourne les parcs visibles publiquement disposant de coordonnées pour une carte.
    /// </summary>
    Task<IReadOnlyCollection<Park>> GetVisibleMapPointsAsync(string? searchTerm, ClosedEntityFilter closedFilter, CancellationToken cancellationToken);

    /// <summary>
    /// Retourne les parcs visibles publiquement disposant de coordonnées pour une carte selon des critères unifiés.
    /// </summary>
    Task<IReadOnlyCollection<Park>> GetVisibleMapPointsAsync(ParkSearchCriteria criteria, ClosedEntityFilter closedFilter, CancellationToken cancellationToken);

    /// <summary>
    /// Retourne les parcs visibles publiquement disposant de coordonnées exploitables.
    /// </summary>
    Task<IReadOnlyCollection<Park>> GetVisibleWithValidCoordinatesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retourne une sélection aléatoire de parcs visibles publiquement.
    /// </summary>
    Task<IReadOnlyCollection<Park>> GetRandomVisibleAsync(int limit, ClosedEntityFilter closedFilter, CancellationToken cancellationToken);

    /// <summary>
    /// Retourne une sélection aléatoire de parcs visibles publiquement, en excluant certains parcs.
    /// </summary>
    Task<IReadOnlyCollection<Park>> GetRandomVisibleAsync(int limit, IReadOnlyCollection<string> excludedParkIds, ClosedEntityFilter closedFilter, CancellationToken cancellationToken);

    /// <summary>
    /// Retourne les parcs visibles mis en avant manuellement sur la home publique.
    /// </summary>
    Task<IReadOnlyCollection<Park>> GetManualHomeFeaturedVisibleAsync(int limit, IReadOnlyCollection<string> excludedParkIds, ClosedEntityFilter closedFilter, CancellationToken cancellationToken);

    /// <summary>
    /// Compte les pays réellement couverts par les parcs.
    /// </summary>
    Task<int> CountDistinctCountryCodesAsync(bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken);

    /// <summary>
    /// Compte les pays réellement couverts par une sélection explicite de parcs.
    /// </summary>
    Task<int> CountDistinctCountryCodesForParkIdsAsync(IReadOnlyCollection<string> parkIds, CancellationToken cancellationToken);

    /// <summary>
    /// Recherche des parcs par nom.
    /// </summary>
    Task<PagedResult<Park>> SearchByNameAsync(string name, int page, int pageSize, bool includeHidden, CancellationToken cancellationToken);

    /// <summary>
    /// Recherche des parcs par critères publics unifiés.
    /// </summary>
    Task<PagedResult<Park>> SearchAsync(ParkSearchCriteria criteria, int page, int pageSize, bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, bool? hasValidCoordinates, ClosedEntityFilter closedFilter, CancellationToken cancellationToken, ParkAdminSortField sortField = ParkAdminSortField.Default, bool sortDescending = false);

    /// <summary>
    /// Recherche des parcs par position.
    /// </summary>
    Task<IReadOnlyCollection<Park>> SearchByLocationAsync(double latitude, double longitude, double radiusInKilometers, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken);

    /// <summary>
    /// Retourne les parcs les plus proches d'une position.
    /// </summary>
    Task<IReadOnlyCollection<Park>> GetNearestByLocationAsync(double latitude, double longitude, int limit, double? maxDistanceInKilometers, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken);

    /// <summary>
    /// Crée un parc.
    /// </summary>
    Task<Park> CreateAsync(Park park, CancellationToken cancellationToken);

    /// <summary>
    /// Met à jour un parc.
    /// </summary>
    Task<Park?> UpdateAsync(string parkId, Park park, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string parkId, CancellationToken cancellationToken);

    /// <summary>
    /// Met à jour uniquement la visibilité d'un parc.
    /// </summary>
    Task<Park?> UpdateVisibilityAsync(string parkId, bool isVisible, CancellationToken cancellationToken);

    /// <summary>
    /// Applique une action de masse d'administration aux parcs.
    /// </summary>
    Task<int> UpdateBulkAdministrationAsync(IReadOnlyCollection<string> parkIds, bool? isVisible, AdminReviewStatus? adminReviewStatus, CancellationToken cancellationToken);

    /// <summary>
    /// Retourne les identifiants de parcs correspondant aux filtres d'administration.
    /// </summary>
    Task<IReadOnlyCollection<string>> GetAdministrationIdsAsync(bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, bool? hasValidCoordinates, CancellationToken cancellationToken, ParkAudienceClassificationFilter? audienceClassificationFilter = null);
}
