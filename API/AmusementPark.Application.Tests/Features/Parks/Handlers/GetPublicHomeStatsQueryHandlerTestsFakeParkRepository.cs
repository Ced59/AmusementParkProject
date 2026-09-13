using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Common.Requests;
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

internal sealed class GetPublicHomeStatsQueryHandlerTestsFakeParkRepository : IParkRepository
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

    public Task<long> CountAsync(bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        return this.CountAsync(includeHidden, cancellationToken);
    }

    public Task<int> CountDistinctCountryCodesAsync(bool includeHidden, CancellationToken cancellationToken)
    {
        this.CountryCountIncludeHiddenCalls.Add(includeHidden);
        return Task.FromResult(this.CountriesCount);
    }

    public Task<int> CountDistinctCountryCodesAsync(bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        return this.CountDistinctCountryCodesAsync(includeHidden, cancellationToken);
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

    public Task<PagedResult<Park>> GetPageAsync(int page, int pageSize, bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, bool? hasValidCoordinates, ClosedEntityFilter closedFilter, CancellationToken cancellationToken, ParkAdminSortField sortField = ParkAdminSortField.Default, bool sortDescending = false, ParkAudienceClassificationFilter? audienceClassificationFilter = null)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Park>> GetVisibleMapPointsAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Park>> GetVisibleMapPointsAsync(string? searchTerm, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Park>> GetVisibleMapPointsAsync(ParkSearchCriteria criteria, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Park>> GetVisibleMapPointsAsync(ParkSearchCriteria criteria, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Park>> GetRandomVisibleAsync(int limit, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Park>> GetRandomVisibleAsync(int limit, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Park>> GetRandomVisibleAsync(int limit, IReadOnlyCollection<string> excludedParkIds, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Park>> GetRandomVisibleAsync(int limit, IReadOnlyCollection<string> excludedParkIds, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Park>> GetManualHomeFeaturedVisibleAsync(int limit, IReadOnlyCollection<string> excludedParkIds, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Park>> GetManualHomeFeaturedVisibleAsync(int limit, IReadOnlyCollection<string> excludedParkIds, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Park>> GetLatestVisibleAsync(int limit, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
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

    public Task<PagedResult<Park>> SearchAsync(ParkSearchCriteria criteria, int page, int pageSize, bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, bool? hasValidCoordinates, ClosedEntityFilter closedFilter, CancellationToken cancellationToken, ParkAdminSortField sortField = ParkAdminSortField.Default, bool sortDescending = false)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Park>> SearchByLocationAsync(double latitude, double longitude, double radiusInKilometers, bool includeHidden, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Park>> SearchByLocationAsync(double latitude, double longitude, double radiusInKilometers, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Park>> GetNearestByLocationAsync(double latitude, double longitude, int limit, double? maxDistanceInKilometers, bool includeHidden, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyCollection<Park>> GetNearestByLocationAsync(double latitude, double longitude, int limit, double? maxDistanceInKilometers, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
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

    public Task<IReadOnlyCollection<string>> GetAdministrationIdsAsync(bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, bool? hasValidCoordinates, CancellationToken cancellationToken, ParkAudienceClassificationFilter? audienceClassificationFilter = null)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAsync(string parkId, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }
}
