using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Handlers;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Images;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Images.Handlers;

public sealed class UpdateImagesBulkMetadataCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCategoryIsNotPatched_ShouldUseRepositoryBulkUpdate()
    {
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<ICommandHandler<UpdateImageMetadataCommand, ApplicationResult<Image>>> updateImageMetadataCommandHandler = new Mock<ICommandHandler<UpdateImageMetadataCommand, ApplicationResult<Image>>>(MockBehavior.Strict);

        ImageBulkMetadataUpdate metadata = new ImageBulkMetadataUpdate(IsPublished: false);
        imageRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "image-1", "image-2" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Image { Id = "image-1", Category = ImageCategory.Park },
                new Image { Id = "image-2", Category = ImageCategory.Park },
            });
        imageRepository
            .Setup(repository => repository.UpdateBulkMetadataAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "image-1", "image-2" })),
                metadata,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        UpdateImagesBulkMetadataCommandHandler handler = new UpdateImagesBulkMetadataCommandHandler(
            imageRepository.Object,
            updateImageMetadataCommandHandler.Object);

        ApplicationResult<BulkAdministrationUpdateResult> result = await handler.HandleAsync(new UpdateImagesBulkMetadataCommand(
            new[] { "image-1", "image-2", "image-1", " " },
            metadata));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.RequestedCount);
        Assert.Equal(2, result.Value.UpdatedCount);
        imageRepository.VerifyAll();
        updateImageMetadataCommandHandler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenCategoryIsPatched_ShouldUseSingleImageMetadataFlow()
    {
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<ICommandHandler<UpdateImageMetadataCommand, ApplicationResult<Image>>> updateImageMetadataCommandHandler = new Mock<ICommandHandler<UpdateImageMetadataCommand, ApplicationResult<Image>>>(MockBehavior.Strict);
        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);

        Image existing = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Logo,
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Description = "Logo",
            TagIds = new List<string> { "keep", "remove" },
            IsCurrent = true,
            IsPublished = true,
            SourceUrl = "https://cdn.example.test/logo.png",
        };
        Image updated = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Park,
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Description = "Logo",
            TagIds = new List<string> { "add", "keep" },
            IsPublished = false,
            SourceUrl = "https://cdn.example.test/logo.png",
        };

        updateImageMetadataCommandHandler
            .Setup(handler => handler.HandleAsync(
                It.Is<UpdateImageMetadataCommand>(command =>
                    command.ImageId == "image-1" &&
                    command.Metadata.Category == ImageCategory.Park &&
                    command.Metadata.OwnerType == ImageOwnerType.Park &&
                    command.Metadata.OwnerId == "park-1" &&
                    command.Metadata.IsCurrent == null &&
                    command.Metadata.IsPublished == false &&
                    command.SuppressSeoNotification &&
                    command.Metadata.SourceUrl == "https://cdn.example.test/logo.png" &&
                    command.Metadata.TagIds.OrderBy(static tagId => tagId).SequenceEqual(new[] { "add", "keep" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<Image>.Success(updated));

        imageRepository
            .SetupSequence(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "image-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { existing })
            .ReturnsAsync(new[] { updated });

        publicSeoUpdateNotifier
            .Setup(notifier => notifier.NotifyAsync(
                It.Is<PublicSeoUpdate>(update =>
                    update.PreviousImages.Count == 1 &&
                    update.CurrentImages.Count == 1 &&
                    update.PreviousImages.Single().Category == ImageCategory.Logo &&
                    update.CurrentImages.Single().Category == ImageCategory.Park),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        UpdateImagesBulkMetadataCommandHandler handler = new UpdateImagesBulkMetadataCommandHandler(
            imageRepository.Object,
            updateImageMetadataCommandHandler.Object,
            publicSeoUpdateNotifier.Object);

        ApplicationResult<BulkAdministrationUpdateResult> result = await handler.HandleAsync(new UpdateImagesBulkMetadataCommand(
            new[] { "image-1" },
            new ImageBulkMetadataUpdate(
                IsPublished: false,
                Category: ImageCategory.Park,
                AddTagIds: new[] { "add" },
                RemoveTagIds: new[] { "remove" })));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.RequestedCount);
        Assert.Equal(1, result.Value.UpdatedCount);
        imageRepository.VerifyAll();
        updateImageMetadataCommandHandler.VerifyAll();
        publicSeoUpdateNotifier.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenSelectionContainsCommentImage_ShouldRejectBeforeMutation()
    {
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "comment-image" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new Image
                {
                    Id = "comment-image",
                    Category = ImageCategory.Comment,
                    OwnerType = ImageOwnerType.Comment,
                    OwnerId = "comment-1",
                },
            });
        Mock<ICommandHandler<UpdateImageMetadataCommand, ApplicationResult<Image>>> updateHandler =
            new Mock<ICommandHandler<UpdateImageMetadataCommand, ApplicationResult<Image>>>(MockBehavior.Strict);
        UpdateImagesBulkMetadataCommandHandler handler = new UpdateImagesBulkMetadataCommandHandler(
            imageRepository.Object,
            updateHandler.Object);

        ApplicationResult<BulkAdministrationUpdateResult> result = await handler.HandleAsync(
            new UpdateImagesBulkMetadataCommand(
                new[] { "comment-image" },
                new ImageBulkMetadataUpdate(IsPublished: false)));

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            static error => error.Code == "image.comment.lifecycle-managed");
        imageRepository.VerifyAll();
        updateHandler.VerifyNoOtherCalls();
    }
}
