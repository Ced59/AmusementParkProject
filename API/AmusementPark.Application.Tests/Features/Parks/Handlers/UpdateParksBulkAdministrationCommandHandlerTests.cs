using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems;
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

public sealed class UpdateParksBulkAdministrationCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenFilterScopeIsProvided_ShouldResolveIdsBeforeBulkUpdate()
    {
        FakeParkRepository parkRepository = new FakeParkRepository
        {
            AdministrationIds = new[] { "park-1", " park-2 ", "park-1", string.Empty },
            UpdatedCount = 2
        };
        FakeParkItemRepository parkItemRepository = new FakeParkItemRepository();
        FakeSearchProjectionWriter searchProjectionWriter = new FakeSearchProjectionWriter();
        UpdateParksBulkAdministrationCommandHandler handler = new UpdateParksBulkAdministrationCommandHandler(
            parkRepository,
            parkItemRepository,
            searchProjectionWriter,
            new NoOpPublicSeoUpdateNotifier());

        ApplicationResult<BulkAdministrationUpdateResult> result = await handler.HandleAsync(
            new UpdateParksBulkAdministrationCommand(
                Array.Empty<string>(),
                true,
                null,
                null,
                AdminReviewStatus.ToReview,
                ParkType.ThemePark,
                "FR",
                true));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.RequestedCount);
        Assert.Equal(2, result.Value.UpdatedCount);
        Assert.Single(parkRepository.GetAdministrationIdsCalls);
        Assert.Equal(new[] { "park-1", "park-2" }, parkRepository.UpdateCalls.Single().ParkIds);
        Assert.Equal(new[] { "parks:park-1", "parks:park-2" }, searchProjectionWriter.UpsertCalls);
    }

    [Fact]
    public async Task HandleAsync_WhenNoIdsAndNoFilterScope_ShouldFail()
    {
        FakeParkRepository parkRepository = new FakeParkRepository();
        UpdateParksBulkAdministrationCommandHandler handler = new UpdateParksBulkAdministrationCommandHandler(
            parkRepository,
            new FakeParkItemRepository(),
            new FakeSearchProjectionWriter(),
            new NoOpPublicSeoUpdateNotifier());

        ApplicationResult<BulkAdministrationUpdateResult> result = await handler.HandleAsync(
            new UpdateParksBulkAdministrationCommand(Array.Empty<string>(), true, null));

        Assert.False(result.IsSuccess);
        Assert.Empty(parkRepository.GetAdministrationIdsCalls);
        Assert.Empty(parkRepository.UpdateCalls);
    }

    private sealed record GetAdministrationIdsCall(
        bool IncludeHidden,
        bool? IsVisible,
        AdminReviewStatus? AdminReviewStatus,
        ParkType? Type,
        string? CountryCode,
        bool? HasValidCoordinates);

    private sealed record UpdateBulkCall(IReadOnlyCollection<string> ParkIds, bool? IsVisible, AdminReviewStatus? AdminReviewStatus);

    private sealed class FakeParkRepository : IParkRepository
    {
        public IReadOnlyCollection<string> AdministrationIds { get; init; } = Array.Empty<string>();
        public int UpdatedCount { get; init; }
        public List<GetAdministrationIdsCall> GetAdministrationIdsCalls { get; } = new List<GetAdministrationIdsCall>();
        public List<UpdateBulkCall> UpdateCalls { get; } = new List<UpdateBulkCall>();
        public List<IReadOnlyCollection<string>> GetByIdsCalls { get; } = new List<IReadOnlyCollection<string>>();

        public Task<IReadOnlyCollection<string>> GetAdministrationIdsAsync(bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, bool? hasValidCoordinates, CancellationToken cancellationToken)
        {
            this.GetAdministrationIdsCalls.Add(new GetAdministrationIdsCall(includeHidden, isVisible, adminReviewStatus, type, countryCode, hasValidCoordinates));
            return Task.FromResult(this.AdministrationIds);
        }

        public Task<int> UpdateBulkAdministrationAsync(IReadOnlyCollection<string> parkIds, bool? isVisible, AdminReviewStatus? adminReviewStatus, CancellationToken cancellationToken)
        {
            this.UpdateCalls.Add(new UpdateBulkCall(parkIds.ToList(), isVisible, adminReviewStatus));
            return Task.FromResult(this.UpdatedCount);
        }

        public Task<Park?> GetByIdAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<Park>> GetByIdsAsync(IEnumerable<string> parkIds, CancellationToken cancellationToken)
        {
            List<string> ids = parkIds.ToList();
            this.GetByIdsCalls.Add(ids);
            IReadOnlyCollection<Park> parks = ids
                .Select(static parkId => new Park
                {
                    Id = parkId,
                    Name = $"Park {parkId}",
                    IsVisible = true,
                    AdminReviewStatus = AdminReviewStatus.Validated,
                })
                .ToList();
            return Task.FromResult(parks);
        }

        public Task<PagedResult<Park>> GetPageAsync(int page, int pageSize, bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, bool? hasValidCoordinates, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<long> CountAsync(bool includeHidden, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyCollection<string>> GetVisibleParkIdsAsync(CancellationToken cancellationToken)
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

        public Task<int> CountDistinctCountryCodesAsync(bool includeHidden, CancellationToken cancellationToken)
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
    }

    private sealed class FakeParkItemRepository : IParkItemRepository
    {
        public Task<IReadOnlyCollection<ParkItem>> GetByParkIdAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<ParkItem>>(Array.Empty<ParkItem>());
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

    private sealed class FakeSearchProjectionWriter : ISearchProjectionWriter
    {
        public List<string> UpsertCalls { get; } = new List<string>();

        public Task UpsertAsync(string resourceType, string resourceId, CancellationToken cancellationToken)
        {
            this.UpsertCalls.Add($"{resourceType}:{resourceId}");
            return Task.CompletedTask;
        }

        public Task UpsertManyAsync(string resourceType, IReadOnlyCollection<string> resourceIds, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAsync(string resourceType, string resourceId, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class NoOpPublicSeoUpdateNotifier : IPublicSeoUpdateNotifier
    {
        public Task NotifyAsync(PublicSeoUpdate update, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
