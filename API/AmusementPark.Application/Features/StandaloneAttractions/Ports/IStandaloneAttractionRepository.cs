using AmusementPark.Application.Common.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.StandaloneAttractions.Ports;

public interface IStandaloneAttractionRepository
{
    Task<StandaloneAttraction?> GetByIdAsync(string id, bool includeHidden, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<StandaloneAttraction>> GetByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken);

    Task<StandaloneAttraction?> FindByLegacyAsync(string? legacyParkId, string? legacyParkItemId, CancellationToken cancellationToken);

    Task<PagedResult<StandaloneAttraction>> GetPageAsync(
        int page,
        int pageSize,
        string? search,
        bool includeHidden,
        bool? isVisible,
        AdminReviewStatus? adminReviewStatus,
        ParkItemType? type,
        string? countryCode,
        string? manufacturerId,
        CancellationToken cancellationToken,
        StandaloneAttractionAdminSortField sortField = StandaloneAttractionAdminSortField.Default,
        bool sortDescending = false);

    Task<IReadOnlyCollection<StandaloneAttraction>> GetPublicSitemapCandidatesAsync(int limit, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<string>> GetIdsByOperatorIdAsync(string operatorId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<string>> GetIdsByManufacturerIdAsync(string manufacturerId, CancellationToken cancellationToken);

    Task<StandaloneAttraction> CreateAsync(StandaloneAttraction attraction, CancellationToken cancellationToken);

    Task<StandaloneAttraction?> UpdateAsync(string id, StandaloneAttraction attraction, CancellationToken cancellationToken);

    Task<int> UpdateBulkAdministrationAsync(IReadOnlyCollection<string> ids, bool? isVisible, AdminReviewStatus? adminReviewStatus, CancellationToken cancellationToken);
}
