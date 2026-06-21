using AmusementPark.Application.Common.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.AttractionManufacturers.Ports;

/// <summary>
/// Port applicatif de persistance des attraction manufacturers.
/// </summary>
public interface IAttractionManufacturerRepository
{
    /// <summary>
    /// Retourne tous les attraction manufacturers.
    /// </summary>
    Task<IReadOnlyCollection<AttractionManufacturer>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retourne une page de attraction manufacturers.
    /// </summary>
    Task<PagedResult<AttractionManufacturer>> GetPageAsync(int page, int pageSize, string? search, CancellationToken cancellationToken);

    /// <summary>
    /// Retourne un attraction manufacturer par identifiant.
    /// </summary>
    Task<AttractionManufacturer?> GetByIdAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// Crée un attraction manufacturer.
    /// </summary>
    Task<AttractionManufacturer> CreateAsync(AttractionManufacturer entity, CancellationToken cancellationToken);

    /// <summary>
    /// Met à jour un attraction manufacturer existant.
    /// </summary>
    Task<AttractionManufacturer?> UpdateAsync(string id, AttractionManufacturer entity, CancellationToken cancellationToken);

    /// <summary>
    /// Met à jour en masse le statut de revue admin.
    /// </summary>
    Task<int> UpdateBulkAdminReviewStatusAsync(IReadOnlyCollection<string> ids, AdminReviewStatus adminReviewStatus, CancellationToken cancellationToken);
}
