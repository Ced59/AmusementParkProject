using AmusementPark.Application.Features.ParkGraphUpserts.Handlers;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Handlers;

public sealed class ListParkGraphUpsertHistoryQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenLimitIsTooHigh_ShouldClampLimitAndTrimTargetParkId()
    {
        Mock<IParkGraphUpsertHistoryRepository> repository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.ListRecentAsync(
                It.Is<ParkGraphUpsertHistoryQuery>(query => query.TargetParkId == "park-1" && query.Limit == 50),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkGraphUpsertHistoryEntry>());

        ListParkGraphUpsertHistoryQueryHandler handler = new ListParkGraphUpsertHistoryQueryHandler(repository.Object);

        IReadOnlyCollection<ParkGraphUpsertHistoryEntry> result = await handler.HandleAsync(
            new ListParkGraphUpsertHistoryQuery(" park-1 ", 200),
            CancellationToken.None);

        Assert.Empty(result);
        repository.VerifyAll();
    }
}
