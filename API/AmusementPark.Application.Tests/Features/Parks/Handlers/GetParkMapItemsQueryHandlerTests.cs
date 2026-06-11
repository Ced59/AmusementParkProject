using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Parks.Handlers;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Parks.Handlers;

public sealed class GetParkMapItemsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenParkIdIsBlank_ShouldFailWithoutCallingRepository()
    {
        FakeParkMapItemsReadRepository repository = new FakeParkMapItemsReadRepository();
        GetParkMapItemsQueryHandler handler = new GetParkMapItemsQueryHandler(repository);

        ApplicationResult<ParkMapItemsResult> result = await handler.HandleAsync(new GetParkMapItemsQuery("   "));

        Assert.False(result.IsSuccess);
        Assert.Empty(repository.Calls);
        Assert.Contains(result.Errors, static error => error.Code == "park.not-found");
    }

    [Fact]
    public async Task HandleAsync_WhenMapItemsExist_ShouldTrimIdAndReturnResult()
    {
        ParkMapItemsResult mapItems = new ParkMapItemsResult
        {
            Park = CreatePark("park-1"),
            Items = new List<ParkMapItemResult>
            {
                new ParkMapItemResult
                {
                    Id = "item-1",
                    Name = "Ride",
                    Category = ParkItemCategory.Attraction,
                    Type = ParkItemType.RollerCoaster,
                    Latitude = 48.8,
                    Longitude = 2.3,
                },
            },
        };
        FakeParkMapItemsReadRepository repository = new FakeParkMapItemsReadRepository
        {
            MapItems = mapItems,
        };
        GetParkMapItemsQueryHandler handler = new GetParkMapItemsQueryHandler(repository);

        ApplicationResult<ParkMapItemsResult> result = await handler.HandleAsync(new GetParkMapItemsQuery(" park-1 ", true));

        Assert.True(result.IsSuccess);
        Assert.Same(mapItems, result.Value);
        Assert.Equal(new[] { new MapItemsCall("park-1", true) }, repository.Calls);
    }

    [Fact]
    public async Task HandleAsync_WhenMapItemsDoNotExist_ShouldFail()
    {
        FakeParkMapItemsReadRepository repository = new FakeParkMapItemsReadRepository();
        GetParkMapItemsQueryHandler handler = new GetParkMapItemsQueryHandler(repository);

        ApplicationResult<ParkMapItemsResult> result = await handler.HandleAsync(new GetParkMapItemsQuery("park-404"));

        Assert.False(result.IsSuccess);
        Assert.Equal(new[] { new MapItemsCall("park-404", false) }, repository.Calls);
    }

    private static Park CreatePark(string id)
    {
        Park park = new Park
        {
            Id = id,
            Name = "Test park",
            IsVisible = true,
        };
        park.SetPosition(48.8, 2.3);
        return park;
    }

    private sealed record MapItemsCall(string ParkId, bool IncludeHidden);

    private sealed class FakeParkMapItemsReadRepository : IParkMapItemsReadRepository
    {
        public ParkMapItemsResult? MapItems { get; init; }

        public List<MapItemsCall> Calls { get; } = new List<MapItemsCall>();

        public Task<ParkMapItemsResult?> GetAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
        {
            this.Calls.Add(new MapItemsCall(parkId, includeHidden));
            return Task.FromResult(this.MapItems);
        }
    }
}
