using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkItems.Contracts;
using AmusementPark.Application.Features.ParkItems.Handlers;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkItems.Queries;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkItems.Handlers;

public sealed class ParkItemNavigationQueryHandlersTests
{
    [Fact]
    public async Task GetSiblingNavigation_ShouldReturnPreviousAndNextAroundCurrentItem()
    {
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        ParkItem currentItem = CreateParkItem("item-2", "Taron");

        parkItemRepository
            .Setup(repository => repository.GetByIdAsync("item-2", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentItem);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Phantasialand", IsVisible = true });
        parkItemRepository
            .Setup(repository => repository.GetNavigationItemsByParkIdAsync("park-1", false, ClosedEntityFilter.OpenOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ParkItemSiblingNavigationItem>
            {
                new ParkItemSiblingNavigationItem { Id = "item-1", Name = "Raik" },
                new ParkItemSiblingNavigationItem { Id = "item-2", Name = "Taron" },
                new ParkItemSiblingNavigationItem { Id = "item-3", Name = "Talocan" },
            });

        GetParkItemSiblingNavigationQueryHandler handler = new GetParkItemSiblingNavigationQueryHandler(parkItemRepository.Object, parkRepository.Object);

        ApplicationResult<ParkItemSiblingNavigationResult> result = await handler.HandleAsync(
            new GetParkItemSiblingNavigationQuery("item-2"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.CurrentPosition);
        Assert.Equal(3, result.Value.TotalItems);
        Assert.Equal(1, result.Value.RemainingItems);
        Assert.Equal("item-1", result.Value.Previous?.Id);
        Assert.Equal("Raik", result.Value.Previous?.Name);
        Assert.Equal("item-3", result.Value.Next?.Id);
        Assert.Equal("Talocan", result.Value.Next?.Name);
        parkItemRepository.VerifyAll();
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task GetRelatedParkItems_ShouldClampLimitBeforeCallingRepository()
    {
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        ParkItem currentItem = CreateParkItem("item-2", "Taron");

        parkItemRepository
            .Setup(repository => repository.GetByIdAsync("item-2", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentItem);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Phantasialand", IsVisible = true });
        parkItemRepository
            .Setup(repository => repository.GetRelatedItemsAsync(currentItem, 6, false, ClosedEntityFilter.OpenOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateParkItem("item-3", "Talocan") });

        GetRelatedParkItemsQueryHandler handler = new GetRelatedParkItemsQueryHandler(parkItemRepository.Object, parkRepository.Object);

        ApplicationResult<IReadOnlyCollection<ParkItem>> result = await handler.HandleAsync(
            new GetRelatedParkItemsQuery("item-2", 99),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        parkItemRepository.VerifyAll();
        parkRepository.VerifyAll();
    }

    private static ParkItem CreateParkItem(string id, string name)
    {
        return new ParkItem
        {
            Id = id,
            ParkId = "park-1",
            Name = name,
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster,
            ZoneId = "zone-1",
            IsVisible = true,
        };
    }
}
