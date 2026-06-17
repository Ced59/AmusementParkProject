using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Videos.Commands;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Application.Features.Videos.Handlers;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Core.Domain.Videos;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Videos.Handlers;

public sealed class CreateVideoCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenOwnerIsInvalid_ShouldFailWithoutResolvingMetadata()
    {
        Mock<IVideoRepository> repository = new Mock<IVideoRepository>(MockBehavior.Strict);
        Mock<IVideoMetadataProvider> metadataProvider = new Mock<IVideoMetadataProvider>(MockBehavior.Strict);
        Mock<IVideoThumbnailImporter> thumbnailImporter = new Mock<IVideoThumbnailImporter>(MockBehavior.Strict);
        CreateVideoCommandHandler handler = new CreateVideoCommandHandler(repository.Object, metadataProvider.Object, thumbnailImporter.Object);

        ApplicationResult<Video> result = await handler.HandleAsync(new CreateVideoCommand(new VideoWriteModel
        {
            OriginalUrl = "https://www.youtube.com/watch?v=abcdefghijk",
            OwnerType = VideoOwnerType.None,
            OwnerId = string.Empty,
        }));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "video.owner.invalid");
    }

    [Fact]
    public async Task HandleAsync_WhenMetadataAndThumbnailAreResolved_ShouldCreateVideoAndPersistThumbnailImageId()
    {
        ResolvedVideoMetadata metadata = new ResolvedVideoMetadata
        {
            HostingProvider = VideoHostingProvider.YouTube,
            OriginalUrl = "https://youtu.be/abcdefghijk",
            CanonicalUrl = "https://www.youtube.com/watch?v=abcdefghijk",
            EmbedUrl = "https://www.youtube.com/embed/abcdefghijk",
            ExternalId = "abcdefghijk",
            Title = "Ride video",
            CreatorName = "Creator",
            ThumbnailUrl = "https://i.ytimg.com/vi/abcdefghijk/hqdefault.jpg",
            MetadataSource = "youtube-data-api",
        };

        Mock<IVideoRepository> repository = new Mock<IVideoRepository>(MockBehavior.Strict);
        Mock<IVideoMetadataProvider> metadataProvider = new Mock<IVideoMetadataProvider>(MockBehavior.Strict);
        Mock<IVideoThumbnailImporter> thumbnailImporter = new Mock<IVideoThumbnailImporter>(MockBehavior.Strict);

        metadataProvider
            .Setup(provider => provider.ResolveAsync("https://youtu.be/abcdefghijk", It.IsAny<CancellationToken>()))
            .ReturnsAsync(metadata);

        repository
            .Setup(repo => repo.CreateAsync(It.IsAny<Video>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Video video, CancellationToken _) =>
            {
                video.Id = "video-1";
                return video;
            });

        thumbnailImporter
            .Setup(importer => importer.ImportAsync(metadata, "video-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync("thumbnail-image-1");

        repository
            .Setup(repo => repo.SetThumbnailImageAsync("video-1", "thumbnail-image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string thumbnailImageId, CancellationToken _) => new Video
            {
                Id = "video-1",
                ThumbnailImageId = thumbnailImageId,
                Title = "Ride video",
            });

        CreateVideoCommandHandler handler = new CreateVideoCommandHandler(repository.Object, metadataProvider.Object, thumbnailImporter.Object);

        ApplicationResult<Video> result = await handler.HandleAsync(new CreateVideoCommand(new VideoWriteModel
        {
            OriginalUrl = "https://youtu.be/abcdefghijk",
            OwnerType = VideoOwnerType.Park,
            OwnerId = "park-1",
            Type = VideoType.OnRide,
            IsPublished = true,
        }));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("thumbnail-image-1", result.Value!.ThumbnailImageId);
        repository.Verify(repo => repo.CreateAsync(It.Is<Video>(video =>
            video.HostingProvider == VideoHostingProvider.YouTube &&
            video.Title == "Ride video" &&
            video.OwnerType == VideoOwnerType.Park &&
            video.OwnerId == "park-1" &&
            video.Type == VideoType.OnRide), It.IsAny<CancellationToken>()), Times.Once);
    }
}
