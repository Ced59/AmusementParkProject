using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Parks.Handlers;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Parks.Queries;
using AmusementPark.Application.Features.Parks.Results;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Parks.Handlers;

public sealed class GetLatestHomeParksQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_UsesFeaturedCardLimitAndPreservesRepositoryOrder()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Park latestPark = new Park { Id = "park-latest", Name = "Latest park", IsVisible = true };
        Park previousPark = new Park { Id = "park-previous", Name = "Previous park", IsVisible = true };
        IReadOnlyCollection<Park> parks = new[] { latestPark, previousPark };
        IReadOnlyDictionary<string, IReadOnlyDictionary<ParkItemCategory, int>> counts =
            new Dictionary<string, IReadOnlyDictionary<ParkItemCategory, int>>
            {
                [latestPark.Id] = new Dictionary<ParkItemCategory, int>
                {
                    [ParkItemCategory.Attraction] = 12,
                },
            };

        parkRepository
            .Setup(repository => repository.GetLatestVisibleAsync(3, ClosedEntityFilter.OpenOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(parks);
        parkItemRepository
            .Setup(repository => repository.GetCountsByCategoryForParkIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-latest", "park-previous" })),
                false,
                ClosedEntityFilter.OpenOnly,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(counts);

        GetLatestHomeParksQueryHandler handler = new GetLatestHomeParksQueryHandler(
            parkRepository.Object,
            parkItemRepository.Object);

        ApplicationResult<IReadOnlyCollection<HomeFeaturedParkResult>> result = await handler.HandleAsync(
            new GetLatestHomeParksQuery(20));

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "park-latest", "park-previous" }, result.Value!.Select(static item => item.Park.Id));
        Assert.Equal(12, result.Value!.First().CountsByCategory[ParkItemCategory.Attraction]);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
    }
}
