using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Handlers;
using AmusementPark.Application.Features.Images.Ports;
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

        imageRepository
            .Setup(repository => repository.GetByIdAsync("image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        updateImageMetadataCommandHandler
            .Setup(handler => handler.HandleAsync(
                It.Is<UpdateImageMetadataCommand>(command =>
                    command.ImageId == "image-1" &&
                    command.Metadata.Category == ImageCategory.Park &&
                    command.Metadata.OwnerType == ImageOwnerType.Park &&
                    command.Metadata.OwnerId == "park-1" &&
                    command.Metadata.IsCurrent == null &&
                    command.Metadata.IsPublished == false &&
                    command.Metadata.SourceUrl == "https://cdn.example.test/logo.png" &&
                    command.Metadata.TagIds.OrderBy(static tagId => tagId).SequenceEqual(new[] { "add", "keep" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<Image>.Success(existing));

        UpdateImagesBulkMetadataCommandHandler handler = new UpdateImagesBulkMetadataCommandHandler(
            imageRepository.Object,
            updateImageMetadataCommandHandler.Object);

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
    }
}
