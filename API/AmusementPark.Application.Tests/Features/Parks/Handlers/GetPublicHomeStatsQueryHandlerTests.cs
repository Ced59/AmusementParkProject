using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Application.Features.ParkItems.Contracts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Handlers;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Parks.Handlers;

public sealed class GetPublicHomeStatsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldCountPublicDataWithIndexedRepositoryQueries()
    {
        FakeParkRepository parkRepository = new FakeParkRepository
        {
            ParksCount = 42,
            CountriesCount = 6
        };
        FakeParkItemRepository parkItemRepository = new FakeParkItemRepository
        {
            AttractionCount = 123
        };
        GetPublicHomeStatsQueryHandler handler = new GetPublicHomeStatsQueryHandler(parkRepository, parkItemRepository);

        ApplicationResult<PublicHomeStatsResult> result = await handler.HandleAsync(new GetPublicHomeStatsQuery());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(42, result.Value.ParksCount);
        Assert.Equal(123, result.Value.AttractionsCount);
        Assert.Equal(6, result.Value.CountriesCount);
        Assert.Equal(new[] { false }, parkRepository.CountIncludeHiddenCalls);
        Assert.Equal(new[] { false }, parkRepository.CountryCountIncludeHiddenCalls);
        Assert.Equal(ParkItemCategory.Attraction, parkItemRepository.CountByCategoryCall?.Category);
        Assert.False(parkItemRepository.CountByCategoryCall?.IncludeHidden);
        Assert.Empty(parkRepository.VisibleParkIdsCalls);
    }

    private sealed record CountByCategoryCall(ParkItemCategory Category, bool IncludeHidden);

    private sealed class FakeParkRepository : IParkRepository
    {
        public long ParksCount { get; init; }
        public int CountriesCount { get; init; }
        public List<bool> CountIncludeHiddenCalls { get; } = new List<bool>();
        public List<bool> CountryCountIncludeHiddenCalls { get; } = new List<bool>();
        public List<CancellationToken> VisibleParkIdsCalls { get; } = new List<CancellationToken>();

        public Task<long> CountAsync(bool includeHidden, CancellationToken cancellationToken)
        {
            this.CountIncludeHiddenCalls.Add(includeHidden);
            return Task.FromResult(this.ParksCount);
        }

        public Task<int> CountDistinctCountryCodesAsync(bool includeHidden, CancellationToken cancellationToken)
        {
            this.CountryCountIncludeHiddenCalls.Add(includeHidden);
            return Task.FromResult(this.CountriesCount);
        }

        public Task<IReadOnlyCollection<string>> GetVisibleParkIdsAsync(CancellationToken cancellationToken)
        {
            this.VisibleParkIdsCalls.Add(cancellationToken);
            return Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());
        }

        public Task<IReadOnlyCollection<Park>> GetVisibleWithValidCoordinatesAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<string>> GetParkIdsByOperatorIdAsync(string operatorId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<string>> GetParkIdsByFounderIdAsync(string founderId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Park?> GetByIdAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<Park>> GetByIdsAsync(IEnumerable<string> parkIds, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<Park>> GetPageAsync(int page, int pageSize, bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, bool? hasValidCoordinates, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<Park>> GetVisibleMapPointsAsync(string? searchTerm, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<Park>> GetVisibleMapPointsAsync(ParkSearchCriteria criteria, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<Park>> GetRandomVisibleAsync(int limit, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<Park>> GetRandomVisibleAsync(int limit, IReadOnlyCollection<string> excludedParkIds, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<Park>> GetManualHomeFeaturedVisibleAsync(int limit, IReadOnlyCollection<string> excludedParkIds, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<int> CountDistinctCountryCodesForParkIdsAsync(IReadOnlyCollection<string> parkIds, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<Park>> SearchByNameAsync(string name, int page, int pageSize, bool includeHidden, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<Park>> SearchAsync(ParkSearchCriteria criteria, int page, int pageSize, bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, bool? hasValidCoordinates, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<Park>> SearchByLocationAsync(double latitude, double longitude, double radiusInKilometers, bool includeHidden, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<Park>> GetNearestByLocationAsync(double latitude, double longitude, int limit, double? maxDistanceInKilometers, bool includeHidden, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Park> CreateAsync(Park park, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Park?> UpdateAsync(string parkId, Park park, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Park?> UpdateVisibilityAsync(string parkId, bool isVisible, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<int> UpdateBulkAdministrationAsync(IReadOnlyCollection<string> parkIds, bool? isVisible, AdminReviewStatus? adminReviewStatus, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<string>> GetAdministrationIdsAsync(bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, bool? hasValidCoordinates, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeParkItemRepository : IParkItemRepository
    {
        public long AttractionCount { get; init; }
        public CountByCategoryCall? CountByCategoryCall { get; private set; }

        public Task<long> CountByCategoryAsync(ParkItemCategory category, bool includeHidden, CancellationToken cancellationToken)
        {
            this.CountByCategoryCall = new CountByCategoryCall(category, includeHidden);
            return Task.FromResult(this.AttractionCount);
        }

        public Task<IReadOnlyCollection<ParkItem>> GetByParkIdAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<ParkItemSiblingNavigationItem>> GetNavigationItemsByParkIdAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<ParkItem>> GetRelatedItemsAsync(ParkItem currentItem, int limit, bool includeHidden, CancellationToken cancellationToken)
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

        public Task<long> CountByCategoryForParkIdsAsync(ParkItemCategory category, IReadOnlyCollection<string> parkIds, bool includeHidden, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyDictionary<string, IReadOnlyDictionary<ParkItemCategory, int>>> GetCountsByCategoryForParkIdsAsync(IReadOnlyCollection<string> parkIds, bool includeHidden, CancellationToken cancellationToken)
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

        public Task<IReadOnlyDictionary<string, int>> GetAttractionCountsByManufacturerIdsAsync(IEnumerable<string> manufacturerIds, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
