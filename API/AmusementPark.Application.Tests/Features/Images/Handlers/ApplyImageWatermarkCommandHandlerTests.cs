using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Handlers;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Images.Handlers;

public sealed class ApplyImageWatermarkCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenImageIsAlreadyWatermarked_ShouldReturnWithoutReprocessing()
    {
        Image image = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Park,
            Path = "park/image-1",
            IsWatermarked = true,
        };

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(value => value.GetByIdAsync("image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        Mock<IImageBinaryStorage> imageBinaryStorage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        ApplyImageWatermarkCommandHandler handler = new ApplyImageWatermarkCommandHandler(imageRepository.Object, imageBinaryStorage.Object);

        ApplicationResult<Image> result = await handler.HandleAsync(new ApplyImageWatermarkCommand("image-1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Same(image, result.Value);
        imageRepository.Verify(value => value.MarkWatermarkedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        imageBinaryStorage.Verify(value => value.ApplyWatermarkAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        imageRepository.VerifyAll();
        imageBinaryStorage.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenImageIsLogo_ShouldRejectWatermark()
    {
        Image image = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Logo,
            Path = "logo/image-1",
        };

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(value => value.GetByIdAsync("image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);

        Mock<IImageBinaryStorage> imageBinaryStorage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        ApplyImageWatermarkCommandHandler handler = new ApplyImageWatermarkCommandHandler(imageRepository.Object, imageBinaryStorage.Object);

        ApplicationResult<Image> result = await handler.HandleAsync(new ApplyImageWatermarkCommand("image-1"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, error => error.Code == "image.watermark.not-allowed");
        imageBinaryStorage.Verify(value => value.ApplyWatermarkAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        imageRepository.VerifyAll();
        imageBinaryStorage.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenImageCanBeWatermarked_ShouldRegenerateBinaryAndMarkMetadata()
    {
        Image image = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Park,
            Path = "park/image-1",
            IsWatermarked = false,
        };

        Image updatedImage = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Park,
            Path = "park/image-1",
            IsWatermarked = true,
        };

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(value => value.GetByIdAsync("image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);
        imageRepository
            .Setup(value => value.MarkWatermarkedAsync("image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedImage);

        Mock<IImageBinaryStorage> imageBinaryStorage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        imageBinaryStorage
            .Setup(value => value.ApplyWatermarkAsync("park/image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        ApplyImageWatermarkCommandHandler handler = new ApplyImageWatermarkCommandHandler(imageRepository.Object, imageBinaryStorage.Object);

        ApplicationResult<Image> result = await handler.HandleAsync(new ApplyImageWatermarkCommand("image-1"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsWatermarked);
        imageRepository.VerifyAll();
        imageBinaryStorage.VerifyAll();
    }
}
