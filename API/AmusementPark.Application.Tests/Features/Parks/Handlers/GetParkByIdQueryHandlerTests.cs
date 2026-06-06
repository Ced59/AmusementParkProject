using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Handlers;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Parks.Handlers;

public sealed class GetParkByIdQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenParkIdIsBlank_ShouldFailWithoutCallingRepository()
    {
        FakeParkRepository repository = new FakeParkRepository();
        GetParkByIdQueryHandler handler = new GetParkByIdQueryHandler(repository);

        ApplicationResult<Park> result = await handler.HandleAsync(new GetParkByIdQuery("   "));

        Assert.False(result.IsSuccess);
        Assert.Empty(repository.GetByIdCalls);
        Assert.Contains(result.Errors, static error => error.Code == "park.not-found");
    }

    [Fact]
    public async Task HandleAsync_WhenParkExists_ShouldTrimIdAndReturnPark()
    {
        Park park = CreatePark("park-1");
        FakeParkRepository repository = new FakeParkRepository
        {
            ParkById = park
        };
        GetParkByIdQueryHandler handler = new GetParkByIdQueryHandler(repository);

        ApplicationResult<Park> result = await handler.HandleAsync(new GetParkByIdQuery(" park-1 ", true));

        Assert.True(result.IsSuccess);
        Assert.Same(park, result.Value);
        Assert.Equal(new[] { new GetByIdCall("park-1", true) }, repository.GetByIdCalls);
    }

    [Fact]
    public async Task HandleAsync_WhenParkDoesNotExist_ShouldFail()
    {
        FakeParkRepository repository = new FakeParkRepository();
        GetParkByIdQueryHandler handler = new GetParkByIdQueryHandler(repository);

        ApplicationResult<Park> result = await handler.HandleAsync(new GetParkByIdQuery("park-404"));

        Assert.False(result.IsSuccess);
        Assert.Equal(new[] { new GetByIdCall("park-404", false) }, repository.GetByIdCalls);
    }

    private static Park CreatePark(string id)
    {
        Park park = new Park
        {
            Id = id,
            Name = "Test park",
            IsVisible = true
        };
        park.SetPosition(48.8, 2.3);
        return park;
    }

    private sealed record GetByIdCall(string ParkId, bool IncludeHidden);

    private sealed class FakeParkRepository : IParkRepository
    {
        public Park? ParkById { get; set; }
        public List<GetByIdCall> GetByIdCalls { get; } = new List<GetByIdCall>();

        public Task<Park?> GetByIdAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
        {
            this.GetByIdCalls.Add(new GetByIdCall(parkId, includeHidden));
            return Task.FromResult(this.ParkById);
        }

        public Task<IReadOnlyCollection<Park>> GetByIdsAsync(IEnumerable<string> parkIds, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<Park>> GetPageAsync(int page, int pageSize, bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, CancellationToken cancellationToken)
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

        public Task<PagedResult<Park>> SearchAsync(ParkSearchCriteria criteria, int page, int pageSize, bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, CancellationToken cancellationToken)
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
    }
}
