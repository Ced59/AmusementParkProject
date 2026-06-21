using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.ParkItems.Contracts;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Ports;

/// <summary>
/// Port applicatif de persistance des park items.
/// </summary>
public interface IParkItemRepository
{
    Task<IReadOnlyCollection<ParkItem>> GetByParkIdAsync(string parkId, bool includeHidden, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ParkItem>> GetByParkIdAsync(string parkId, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken);
    Task<PagedResult<ParkItem>> GetPublicPageByParkIdAsync(int page, int pageSize, string parkId, string? search, bool includeHidden, ClosedEntityFilter closedFilter, ParkItemCategory? category, ParkItemType? type, string? zoneId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ParkItem>> GetByParkIdsAsync(IReadOnlyCollection<string> parkIds, bool includeHidden, CancellationToken cancellationToken);
    Task<IReadOnlyList<ParkItemSiblingNavigationItem>> GetNavigationItemsByParkIdAsync(string parkId, bool includeHidden, CancellationToken cancellationToken);
    Task<IReadOnlyList<ParkItemSiblingNavigationItem>> GetNavigationItemsByParkIdAsync(string parkId, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ParkItem>> GetRelatedItemsAsync(ParkItem currentItem, int limit, bool includeHidden, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ParkItem>> GetRelatedItemsAsync(ParkItem currentItem, int limit, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ParkItem>> GetPublicSitemapCandidatesAsync(int limit, CancellationToken cancellationToken);
    Task<PagedResult<ParkItem>> GetPageAsync(int page, int pageSize, string? parkId, string? search, bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkItemCategory? category, ParkItemType? type, string? zoneId, string? manufacturerId, ParkItemContentBacklogFilter? contentBacklogFilter, CancellationToken cancellationToken, ParkItemAdminSortField sortField = ParkItemAdminSortField.Default, bool sortDescending = false);
    Task<long> CountByCategoryAsync(ParkItemCategory category, bool includeHidden, CancellationToken cancellationToken);
    Task<long> CountByCategoryAsync(ParkItemCategory category, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken);
    Task<long> CountByCategoryForParkIdsAsync(ParkItemCategory category, IReadOnlyCollection<string> parkIds, bool includeHidden, CancellationToken cancellationToken);
    Task<long> CountByCategoryForParkIdsAsync(ParkItemCategory category, IReadOnlyCollection<string> parkIds, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, IReadOnlyDictionary<ParkItemCategory, int>>> GetCountsByCategoryForParkIdsAsync(IReadOnlyCollection<string> parkIds, bool includeHidden, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, IReadOnlyDictionary<ParkItemCategory, int>>> GetCountsByCategoryForParkIdsAsync(IReadOnlyCollection<string> parkIds, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, ParkItemVisibilityCounts>> GetVisibilityCountsByParkIdsAsync(IReadOnlyCollection<string> parkIds, CancellationToken cancellationToken);
    Task<ParkItem?> GetByIdAsync(string parkItemId, CancellationToken cancellationToken);
    Task<ParkItem?> GetByIdAsync(string parkItemId, bool includeHidden, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ParkItem>> GetByIdsAsync(IReadOnlyCollection<string> parkItemIds, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> GetParkIdsByManufacturerIdAsync(string manufacturerId, CancellationToken cancellationToken);
    Task<ParkItem> CreateAsync(ParkItem parkItem, CancellationToken cancellationToken);
    Task<ParkItem?> UpdateAsync(string parkItemId, ParkItem parkItem, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string parkItemId, CancellationToken cancellationToken);
    Task<int> UpdateBulkAdministrationAsync(IReadOnlyCollection<string> parkItemIds, bool? isVisible, AdminReviewStatus? adminReviewStatus, CancellationToken cancellationToken);
    Task<int> UpdateBulkFieldsAsync(IReadOnlyCollection<string> parkItemIds, bool updateZone, string? zoneId, ParkItemCategory? category, ParkItemType? type, bool updateManufacturer, string? manufacturerId, bool? isVisible, AdminReviewStatus? adminReviewStatus, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, int>> GetAttractionCountsByManufacturerIdsAsync(IEnumerable<string> manufacturerIds, CancellationToken cancellationToken, bool includeHidden = false);
}
