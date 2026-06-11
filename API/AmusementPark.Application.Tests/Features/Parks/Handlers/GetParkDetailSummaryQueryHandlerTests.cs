using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Handlers;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Parks.Handlers;

public sealed class GetParkDetailSummaryQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenParkIdIsBlank_ShouldFailWithoutCallingRepository()
    {
        FakeParkDetailSummaryReadRepository repository = new FakeParkDetailSummaryReadRepository();
        GetParkDetailSummaryQueryHandler handler = new GetParkDetailSummaryQueryHandler(repository);

        ApplicationResult<ParkDetailSummaryResult> result = await handler.HandleAsync(new GetParkDetailSummaryQuery("   "));

        Assert.False(result.IsSuccess);
        Assert.Empty(repository.Calls);
        Assert.Contains(result.Errors, static error => error.Code == "park.not-found");
    }

    [Fact]
    public async Task HandleAsync_WhenSummaryExists_ShouldTrimIdAndReturnSummary()
    {
        ParkDetailSummaryResult summary = new ParkDetailSummaryResult
        {
            Park = CreatePark("park-1"),
            Stats = new ParkDetailSummaryStatsResult
            {
                TotalItems = 3,
                ZoneCount = 1,
                AttractionCount = 2,
                RestaurantCount = 1,
            },
        };
        FakeParkDetailSummaryReadRepository repository = new FakeParkDetailSummaryReadRepository
        {
            Summary = summary
        };
        GetParkDetailSummaryQueryHandler handler = new GetParkDetailSummaryQueryHandler(repository);

        ApplicationResult<ParkDetailSummaryResult> result = await handler.HandleAsync(new GetParkDetailSummaryQuery(" park-1 ", true));

        Assert.True(result.IsSuccess);
        Assert.Same(summary, result.Value);
        Assert.Equal(new[] { new SummaryCall("park-1", true) }, repository.Calls);
    }

    [Fact]
    public async Task HandleAsync_WhenSummaryDoesNotExist_ShouldFail()
    {
        FakeParkDetailSummaryReadRepository repository = new FakeParkDetailSummaryReadRepository();
        GetParkDetailSummaryQueryHandler handler = new GetParkDetailSummaryQueryHandler(repository);

        ApplicationResult<ParkDetailSummaryResult> result = await handler.HandleAsync(new GetParkDetailSummaryQuery("park-404"));

        Assert.False(result.IsSuccess);
        Assert.Equal(new[] { new SummaryCall("park-404", false) }, repository.Calls);
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

    private sealed record SummaryCall(string ParkId, bool IncludeHidden);

    private sealed class FakeParkDetailSummaryReadRepository : IParkDetailSummaryReadRepository
    {
        public ParkDetailSummaryResult? Summary { get; init; }

        public List<SummaryCall> Calls { get; } = new List<SummaryCall>();

        public Task<ParkDetailSummaryResult?> GetAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
        {
            this.Calls.Add(new SummaryCall(parkId, includeHidden));
            return Task.FromResult(this.Summary);
        }
    }
}
