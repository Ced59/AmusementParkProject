using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Application.Features.ParkItems.Handlers;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkItems.Queries;
using AmusementPark.Application.Features.ParkItems.Results;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkItems.Handlers;

public sealed class GetParkItemsByParkIdQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenRepositoryReturnsPage_ShouldReturnPagedItemsWithMainImages()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);

        ParkItem firstItem = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "First item",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster,
        };
        ParkItem secondItem = new ParkItem
        {
            Id = "item-2",
            ParkId = "park-1",
            Name = "Second item",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster,
        };

        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1" });
        parkItemRepository
            .Setup(repository => repository.GetPublicPageByParkIdAsync(
                2,
                12,
                "park-1",
                "coaster",
                false,
                ClosedEntityFilter.All,
                ParkItemCategory.Attraction,
                ParkItemType.RollerCoaster,
                "zone-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<ParkItem>(
                new[] { firstItem, secondItem },
                2,
                12,
                42));
        imageRepository
            .Setup(repository => repository.GetMainImageIdsByOwnersAsync(
                ImageOwnerType.ParkItem,
                It.Is<IReadOnlyCollection<string>>(ownerIds => ownerIds.SequenceEqual(new[] { "item-1", "item-2" })),
                ImageCategory.ParkItem,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, string>
            {
                ["item-1"] = "image-1",
            });

        GetParkItemsByParkIdQueryHandler handler = new GetParkItemsByParkIdQueryHandler(
            parkItemRepository.Object,
            imageRepository.Object,
            new ParkItemReferenceValidator(
                parkRepository.Object,
                Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
                Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict)));

        ApplicationResult<PagedResult<ParkItemListResult>> result = await handler.HandleAsync(
            new GetParkItemsByParkIdQuery(
                " park-1 ",
                new PagedQuery(2, 12),
                IncludeHidden: false,
                ClosedFilter: ClosedEntityFilter.All,
                Search: "coaster",
                Category: ParkItemCategory.Attraction,
                Type: ParkItemType.RollerCoaster,
                ZoneId: "zone-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(42, result.Value.TotalItems);
        Assert.Equal(2, result.Value.Page);
        Assert.Equal(12, result.Value.PageSize);
        Assert.Collection(
            result.Value.Items,
            item =>
            {
                Assert.Equal("item-1", item.Item.Id);
                Assert.Equal("image-1", item.MainImageId);
            },
            item =>
            {
                Assert.Equal("item-2", item.Item.Id);
                Assert.Null(item.MainImageId);
            });

        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyAll();
    }
}
