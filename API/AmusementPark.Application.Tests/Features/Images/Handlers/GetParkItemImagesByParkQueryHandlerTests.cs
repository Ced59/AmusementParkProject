using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Handlers;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Images.Queries;
using AmusementPark.Application.Features.Images.Results;
using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Images.Handlers;

public sealed class GetParkItemImagesByParkQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenPublicParkItemsHavePublishedImages_ShouldReturnImagesWithTheirSourceItems()
    {
        Mock<IParkRepository> parkRepository = CreateParkRepository();
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        ParkItem item = CreateParkItem("item-1", "Family Ride");
        Image image = CreateImage("image-1", "item-1", true);

        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", false, ClosedEntityFilter.OpenOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });
        imageRepository
            .Setup(repository => repository.GetPageAsync(
                1,
                20,
                It.Is<ImageSearchCriteria>(criteria =>
                    criteria.OwnerType == ImageOwnerType.ParkItem &&
                    criteria.Category == ImageCategory.ParkItem &&
                    criteria.IsPublished == true &&
                    criteria.HasOwner == true &&
                    criteria.OwnerIds != null &&
                    criteria.OwnerIds.SequenceEqual(new[] { "item-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Image>(new[] { image }, 1, 20, 1));
        GetParkItemImagesByParkQueryHandler handler = CreateHandler(parkRepository, parkItemRepository, imageRepository);

        ApplicationResult<PagedResult<ParkItemImageResult>> result = await handler.HandleAsync(
            new GetParkItemImagesByParkQuery(new PagedQuery(1, 20), " park-1 "),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        ParkItemImageResult entry = Assert.Single(result.Value.Items);
        Assert.Equal("image-1", entry.Image.Id);
        Assert.Equal("Family Ride", entry.Item.Name);
        Assert.Equal(1, result.Value.TotalItems);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenIncludeHiddenIsTrue_ShouldUseAllClosedFilterAndKeepPublicationFilterOpen()
    {
        Mock<IParkRepository> parkRepository = CreateParkRepository(expectedIncludeHidden: true);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        ParkItem item = CreateParkItem("item-1", "Draft Ride");
        Image image = CreateImage("image-1", "item-1", false);

        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", true, ClosedEntityFilter.All, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });
        imageRepository
            .Setup(repository => repository.GetPageAsync(
                1,
                20,
                It.Is<ImageSearchCriteria>(criteria =>
                    criteria.OwnerType == ImageOwnerType.ParkItem &&
                    criteria.Category == ImageCategory.ParkItem &&
                    criteria.IsPublished == null &&
                    criteria.HasOwner == true &&
                    criteria.OwnerIds != null &&
                    criteria.OwnerIds.SequenceEqual(new[] { "item-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Image>(new[] { image }, 1, 20, 1));
        GetParkItemImagesByParkQueryHandler handler = CreateHandler(parkRepository, parkItemRepository, imageRepository);

        ApplicationResult<PagedResult<ParkItemImageResult>> result = await handler.HandleAsync(
            new GetParkItemImagesByParkQuery(new PagedQuery(1, 20), "park-1", true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        ParkItemImageResult entry = Assert.Single(result.Value.Items);
        Assert.False(entry.Image.IsPublished);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenParkHasNoPublicItems_ShouldReturnEmptyPageWithoutQueryingImages()
    {
        Mock<IParkRepository> parkRepository = CreateParkRepository();
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);

        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", false, ClosedEntityFilter.OpenOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());
        GetParkItemImagesByParkQueryHandler handler = CreateHandler(parkRepository, parkItemRepository, imageRepository);

        ApplicationResult<PagedResult<ParkItemImageResult>> result = await handler.HandleAsync(
            new GetParkItemImagesByParkQuery(new PagedQuery(1, 20), "park-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalItems);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenPublicParkItemsAreNotRelevant_ShouldReturnEmptyPageWithoutQueryingImages()
    {
        Mock<IParkRepository> parkRepository = CreateParkRepository();
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        ParkItem item = CreateParkItem("item-1", "Hidden Review Item", AdminReviewStatus.NotRelevant);

        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", false, ClosedEntityFilter.OpenOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });
        GetParkItemImagesByParkQueryHandler handler = CreateHandler(parkRepository, parkItemRepository, imageRepository);

        ApplicationResult<PagedResult<ParkItemImageResult>> result = await handler.HandleAsync(
            new GetParkItemImagesByParkQuery(new PagedQuery(1, 20), "park-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalItems);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false, AdminReviewStatus.Validated, ParkStatus.Operating)]
    [InlineData(true, AdminReviewStatus.NotRelevant, ParkStatus.Operating)]
    [InlineData(true, AdminReviewStatus.Validated, ParkStatus.ClosedDefinitively)]
    public async Task HandleAsync_WhenPublicParentParkIsNotPublic_ShouldReturnNotFoundWithoutQueryingItems(
        bool isVisible,
        AdminReviewStatus adminReviewStatus,
        ParkStatus status)
    {
        Mock<IParkRepository> parkRepository = CreateParkRepository(isVisible, adminReviewStatus, status);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        GetParkItemImagesByParkQueryHandler handler = CreateHandler(parkRepository, parkItemRepository, imageRepository);

        ApplicationResult<PagedResult<ParkItemImageResult>> result = await handler.HandleAsync(
            new GetParkItemImagesByParkQuery(new PagedQuery(1, 20), "park-1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "park.not-found");
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
        imageRepository.VerifyNoOtherCalls();
    }

    private static GetParkItemImagesByParkQueryHandler CreateHandler(
        Mock<IParkRepository> parkRepository,
        Mock<IParkItemRepository> parkItemRepository,
        Mock<IImageRepository> imageRepository)
    {
        ParkItemReferenceValidator validator = new ParkItemReferenceValidator(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict));

        return new GetParkItemImagesByParkQueryHandler(parkItemRepository.Object, imageRepository.Object, validator);
    }

    private static Mock<IParkRepository> CreateParkRepository(
        bool isVisible = true,
        AdminReviewStatus adminReviewStatus = AdminReviewStatus.Validated,
        ParkStatus status = ParkStatus.Operating,
        bool expectedIncludeHidden = false)
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", expectedIncludeHidden, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedIncludeHidden || isVisible
                ? new Park { Id = "park-1", Name = "Visible Park", IsVisible = isVisible, AdminReviewStatus = adminReviewStatus, Status = status }
                : null);
        return parkRepository;
    }

    private static ParkItem CreateParkItem(string id, string name, AdminReviewStatus adminReviewStatus = AdminReviewStatus.Validated)
    {
        return new ParkItem
        {
            Id = id,
            ParkId = "park-1",
            Name = name,
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.FlatRide,
            IsVisible = true,
            AdminReviewStatus = adminReviewStatus,
        };
    }

    private static Image CreateImage(string id, string ownerId, bool isPublished)
    {
        return new Image
        {
            Id = id,
            OwnerType = ImageOwnerType.ParkItem,
            OwnerId = ownerId,
            Category = ImageCategory.ParkItem,
            IsPublished = isPublished,
        };
    }
}
