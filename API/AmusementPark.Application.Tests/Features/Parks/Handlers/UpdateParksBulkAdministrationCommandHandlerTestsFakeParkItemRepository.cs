using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Application.Features.ParkItems.Contracts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Commands;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Handlers;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Parks.Handlers;

internal sealed class UpdateParksBulkAdministrationCommandHandlerTestsFakeParkItemRepository : IParkItemRepository
{
    public Task<IReadOnlyCollection<ParkItem>> GetByParkIdAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<ParkItem>>(Array.Empty<ParkItem>());
    }

    public Task<IReadOnlyCollection<ParkItem>> GetByParkIdAsync(string parkId, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        return this.GetByParkIdAsync(parkId, includeHidden, cancellationToken);
    }

    public Task<PagedResult<ParkItem>> GetPublicPageByParkIdAsync(int page, int pageSize, string parkId, string? search, bool includeHidden, ClosedEntityFilter closedFilter, ParkItemCategory? category, ParkItemType? type, string? zoneId, IReadOnlyCollection<string> manufacturerIds, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<ParkItem>> GetByParkIdsAsync(IReadOnlyCollection<string> parkIds, bool includeHidden, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<ParkItem>>(Array.Empty<ParkItem>());
    }

    public Task<IReadOnlyCollection<ParkItem>> GetVisibleOpenAttractionsByParkIdsAsync(IReadOnlyCollection<string> parkIds, CancellationToken cancellationToken)
    {
        return this.GetByParkIdsAsync(parkIds, true, cancellationToken);
    }

    public Task<IReadOnlyList<ParkItemSiblingNavigationItem>> GetNavigationItemsByParkIdAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<ParkItemSiblingNavigationItem>> GetNavigationItemsByParkIdAsync(string parkId, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<ParkItem>> GetRelatedItemsAsync(ParkItem currentItem, int limit, bool includeHidden, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<ParkItem>> GetRelatedItemsAsync(ParkItem currentItem, int limit, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<ParkItem>> GetPublicSitemapCandidatesAsync(int limit, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<PagedResult<ParkItem>> GetPageAsync(int page, int pageSize, string? parkId, string? search, bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkItemCategory? category, ParkItemType? type, string? zoneId, string? manufacturerId, ParkItemContentBacklogFilter? contentBacklogFilter, CancellationToken cancellationToken, ParkItemAdminSortField sortField = ParkItemAdminSortField.Default, bool sortDescending = false)
    {
        throw new NotImplementedException();
    }

    public Task<long> CountByCategoryAsync(ParkItemCategory category, bool includeHidden, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<long> CountByCategoryAsync(ParkItemCategory category, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<long> CountByCategoryForParkIdsAsync(ParkItemCategory category, IReadOnlyCollection<string> parkIds, bool includeHidden, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<long> CountByCategoryForParkIdsAsync(ParkItemCategory category, IReadOnlyCollection<string> parkIds, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyDictionary<string, IReadOnlyDictionary<ParkItemCategory, int>>> GetCountsByCategoryForParkIdsAsync(IReadOnlyCollection<string> parkIds, bool includeHidden, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyDictionary<string, IReadOnlyDictionary<ParkItemCategory, int>>> GetCountsByCategoryForParkIdsAsync(IReadOnlyCollection<string> parkIds, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyDictionary<string, ParkItemVisibilityCounts>> GetVisibilityCountsByParkIdsAsync(IReadOnlyCollection<string> parkIds, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<ParkItem?> GetByIdAsync(string parkItemId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<ParkItem?> GetByIdAsync(string parkItemId, bool includeHidden, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<ParkItem>> GetByIdsAsync(IReadOnlyCollection<string> parkItemIds, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<string>> GetParkIdsByManufacturerIdAsync(string manufacturerId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<ParkItem>> GetByManufacturerIdAsync(string manufacturerId, bool includeHidden, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyCollection<ParkItem>>(Array.Empty<ParkItem>());
    }

    public Task<ParkItem> CreateAsync(ParkItem parkItem, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<ParkItem?> UpdateAsync(string parkItemId, ParkItem parkItem, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAsync(string parkItemId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<int> UpdateBulkAdministrationAsync(IReadOnlyCollection<string> parkItemIds, bool? isVisible, AdminReviewStatus? adminReviewStatus, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<int> UpdateBulkFieldsAsync(IReadOnlyCollection<string> parkItemIds, bool updateZone, string? zoneId, ParkItemCategory? category, ParkItemType? type, bool updateManufacturer, string? manufacturerId, bool? isVisible, AdminReviewStatus? adminReviewStatus, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyDictionary<string, int>> GetAttractionCountsByManufacturerIdsAsync(IEnumerable<string> manufacturerIds, CancellationToken cancellationToken, bool includeHidden = false)
    {
        throw new NotImplementedException();
    }
}
