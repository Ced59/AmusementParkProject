using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Handlers;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Images.Results;
using AmusementPark.Core.Domain.Images;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Images.Handlers;

public sealed class UploadImageCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenParkLogoRequestsWatermark_ShouldSaveWithoutWatermark()
    {
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IImageProcessingPipeline> imageProcessingPipeline = new Mock<IImageProcessingPipeline>(MockBehavior.Strict);
        Mock<IImageBinaryStorage> imageBinaryStorage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        UploadImageCommandHandler handler = new UploadImageCommandHandler(
            imageRepository.Object,
            imageProcessingPipeline.Object,
            imageBinaryStorage.Object);

        FilePayload file = new FilePayload
        {
            FileName = "logo.png",
            ContentType = "image/png",
            Length = 8,
            Content = new MemoryStream(new byte[] { 137, 80, 78, 71, 0, 0, 0, 0 }),
        };

        imageProcessingPipeline
            .Setup(pipeline => pipeline.ExtractMetadataAsync(
                It.Is<ImageUploadRequest>(request => request.Category == ImageCategory.ParkLogo && request.WithWatermark),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImageProcessingMetadata
            {
                Width = 1,
                Height = 1,
                SizeInBytes = file.Length,
            });

        imageBinaryStorage
            .Setup(storage => storage.SaveAsync(
                It.Is<string>(path => path.StartsWith("park_logo/", StringComparison.Ordinal)),
                file,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "park_logo/logo.webp", "park_logo/logo.jpg" });

        imageRepository
            .Setup(repository => repository.CreateAsync(
                It.Is<ImageUploadRequest>(request =>
                    request.Category == ImageCategory.ParkLogo &&
                    !request.WithWatermark &&
                    request.StoragePath != null &&
                    request.StoragePath.StartsWith("park_logo/", StringComparison.Ordinal)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImageUploadRequest request, CancellationToken _) => new Image
            {
                Id = request.ImageId ?? "image-id",
                Category = request.Category,
                OriginalFileName = request.File!.FileName,
                ContentType = request.File.ContentType,
                Path = request.StoragePath,
            });

        ApplicationResult<UploadedImageResult> result = await handler.HandleAsync(new UploadImageCommand(new ImageUploadRequest
        {
            Category = ImageCategory.ParkLogo,
            File = file,
            WithWatermark = true,
        }));

        Assert.True(result.IsSuccess);
        Assert.Equal(ImageCategory.ParkLogo, result.Value?.Image.Category);
        imageProcessingPipeline.VerifyAll();
        imageBinaryStorage.VerifyAll();
        imageRepository.VerifyAll();
    }
}
