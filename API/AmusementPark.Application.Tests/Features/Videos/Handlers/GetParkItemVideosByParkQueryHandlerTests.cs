using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Application.Features.Videos.Handlers;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Application.Features.Videos.Queries;
using AmusementPark.Application.Features.Videos.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Videos;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Videos.Handlers;

public sealed class GetParkItemVideosByParkQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenPublicParkItemsHavePublishedVideos_ShouldReturnVideosWithTheirSourceItems()
    {
        Mock<IParkRepository> parkRepository = CreateParkRepository();
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IVideoRepository> videoRepository = new Mock<IVideoRepository>(MockBehavior.Strict);
        ParkItem item = CreateParkItem("item-1", "Family Ride");
        Video video = CreateVideo("video-1", "item-1", true);

        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", false, ClosedEntityFilter.OpenOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });
        videoRepository
            .Setup(repository => repository.GetPageAsync(
                1,
                20,
                It.Is<VideoSearchCriteria>(criteria =>
                    criteria.OwnerType == VideoOwnerType.ParkItem &&
                    criteria.OwnerId == null &&
                    criteria.OwnerIds != null &&
                    criteria.OwnerIds.SequenceEqual(new[] { "item-1" }) &&
                    criteria.IsPublished == true &&
                    criteria.Type == VideoType.OnRide &&
                    criteria.TagId == "tag-1" &&
                    criteria.CreatorName == "creator" &&
                    criteria.LanguageCode == "fr" &&
                    criteria.SortBy == "published" &&
                    criteria.SortDirection == "desc"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Video>(new[] { video }, 1, 20, 1));
        GetParkItemVideosByParkQueryHandler handler = CreateHandler(parkRepository, parkItemRepository, videoRepository);

        ApplicationResult<PagedResult<ParkItemVideoResult>> result = await handler.HandleAsync(
            new GetParkItemVideosByParkQuery(
                new PagedQuery(1, 20),
                " park-1 ",
                new VideoSearchCriteria(
                    Type: VideoType.OnRide,
                    TagId: "tag-1",
                    CreatorName: "creator",
                    LanguageCode: "fr",
                    SortBy: "published",
                    SortDirection: "desc")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        ParkItemVideoResult entry = Assert.Single(result.Value.Items);
        Assert.Equal("video-1", entry.Video.Id);
        Assert.Equal("Family Ride", entry.Item.Name);
        Assert.Equal(1, result.Value.TotalItems);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        videoRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenIncludeHiddenIsTrue_ShouldUseAllClosedFilterAndKeepPublicationFilterOpen()
    {
        Mock<IParkRepository> parkRepository = CreateParkRepository();
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IVideoRepository> videoRepository = new Mock<IVideoRepository>(MockBehavior.Strict);
        ParkItem item = CreateParkItem("item-1", "Draft Ride");
        Video video = CreateVideo("video-1", "item-1", false);

        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", true, ClosedEntityFilter.All, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });
        videoRepository
            .Setup(repository => repository.GetPageAsync(
                1,
                20,
                It.Is<VideoSearchCriteria>(criteria =>
                    criteria.OwnerType == VideoOwnerType.ParkItem &&
                    criteria.OwnerIds != null &&
                    criteria.OwnerIds.SequenceEqual(new[] { "item-1" }) &&
                    criteria.IsPublished == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Video>(new[] { video }, 1, 20, 1));
        GetParkItemVideosByParkQueryHandler handler = CreateHandler(parkRepository, parkItemRepository, videoRepository);

        ApplicationResult<PagedResult<ParkItemVideoResult>> result = await handler.HandleAsync(
            new GetParkItemVideosByParkQuery(new PagedQuery(1, 20), "park-1", new VideoSearchCriteria(), true),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        ParkItemVideoResult entry = Assert.Single(result.Value.Items);
        Assert.False(entry.Video.IsPublished);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        videoRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenParkHasNoPublicItems_ShouldReturnEmptyPageWithoutQueryingVideos()
    {
        Mock<IParkRepository> parkRepository = CreateParkRepository();
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IVideoRepository> videoRepository = new Mock<IVideoRepository>(MockBehavior.Strict);

        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", false, ClosedEntityFilter.OpenOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());
        GetParkItemVideosByParkQueryHandler handler = CreateHandler(parkRepository, parkItemRepository, videoRepository);

        ApplicationResult<PagedResult<ParkItemVideoResult>> result = await handler.HandleAsync(
            new GetParkItemVideosByParkQuery(new PagedQuery(1, 20), "park-1", new VideoSearchCriteria()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalItems);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        videoRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenPublicParkItemsAreNotRelevant_ShouldReturnEmptyPageWithoutQueryingVideos()
    {
        Mock<IParkRepository> parkRepository = CreateParkRepository();
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IVideoRepository> videoRepository = new Mock<IVideoRepository>(MockBehavior.Strict);
        ParkItem item = CreateParkItem("item-1", "Hidden Review Item", AdminReviewStatus.NotRelevant);

        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", false, ClosedEntityFilter.OpenOnly, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });
        GetParkItemVideosByParkQueryHandler handler = CreateHandler(parkRepository, parkItemRepository, videoRepository);

        ApplicationResult<PagedResult<ParkItemVideoResult>> result = await handler.HandleAsync(
            new GetParkItemVideosByParkQuery(new PagedQuery(1, 20), "park-1", new VideoSearchCriteria()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalItems);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        videoRepository.VerifyNoOtherCalls();
    }

    private static GetParkItemVideosByParkQueryHandler CreateHandler(
        Mock<IParkRepository> parkRepository,
        Mock<IParkItemRepository> parkItemRepository,
        Mock<IVideoRepository> videoRepository)
    {
        ParkItemReferenceValidator validator = new ParkItemReferenceValidator(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict));

        return new GetParkItemVideosByParkQueryHandler(parkItemRepository.Object, videoRepository.Object, validator);
    }

    private static Mock<IParkRepository> CreateParkRepository()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Visible Park", IsVisible = true, AdminReviewStatus = AdminReviewStatus.Validated });
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

    private static Video CreateVideo(string id, string ownerId, bool isPublished)
    {
        return new Video
        {
            Id = id,
            OwnerType = VideoOwnerType.ParkItem,
            OwnerId = ownerId,
            Type = VideoType.OnRide,
            OriginalUrl = "https://www.youtube.com/watch?v=abcdefghijk",
            CanonicalUrl = "https://www.youtube.com/watch?v=abcdefghijk",
            Title = "Ride video",
            IsPublished = isPublished,
        };
    }
}
