using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Handlers;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Images.Handlers;

public sealed class ImportRemoteImageCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenSourceUrlIsInvalid_ShouldFailWithoutImporting()
    {
        Mock<IRemoteImageImporter> remoteImageImporter = new Mock<IRemoteImageImporter>(MockBehavior.Strict);
        ImportRemoteImageCommandHandler handler = CreateHandler(remoteImageImporter);

        ApplicationResult<Image> result = await handler.HandleAsync(new ImportRemoteImageCommand(new RemoteImageImportRequest
        {
            SourceUrl = "ftp://example.test/logo.png",
            Category = ImageCategory.Logo,
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
        }));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "image.remote-import.source-invalid");
        remoteImageImporter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenRequestIsValid_ShouldImportRemoteImage()
    {
        Mock<IRemoteImageImporter> remoteImageImporter = new Mock<IRemoteImageImporter>(MockBehavior.Strict);
        Image importedImage = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Park,
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            SourceUrl = "https://cdn.example.test/photo.webp",
        };

        remoteImageImporter
            .Setup(importer => importer.ImportAsync(
                It.Is<RemoteImageImportRequest>(request =>
                    request.SourceUrl == "https://cdn.example.test/photo.webp" &&
                    request.Category == ImageCategory.Park &&
                    request.OwnerType == ImageOwnerType.Park &&
                    request.OwnerId == "park-1" &&
                    request.Description == "Cover" &&
                    request.WithWatermark &&
                    !request.SetAsCurrent),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(importedImage);

        ImportRemoteImageCommandHandler handler = CreateHandler(remoteImageImporter);

        ApplicationResult<Image> result = await handler.HandleAsync(new ImportRemoteImageCommand(new RemoteImageImportRequest
        {
            SourceUrl = "  https://cdn.example.test/photo.webp  ",
            Category = ImageCategory.Park,
            OwnerType = ImageOwnerType.Park,
            OwnerId = "  park-1  ",
            Description = "  Cover  ",
            WithWatermark = true,
            SetAsCurrent = false,
        }));

        Assert.True(result.IsSuccess);
        Assert.Same(importedImage, result.Value);
        remoteImageImporter.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenLogoRequestsWatermark_ShouldImportWithoutWatermark()
    {
        Mock<IRemoteImageImporter> remoteImageImporter = new Mock<IRemoteImageImporter>(MockBehavior.Strict);
        Image importedImage = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Logo,
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            SourceUrl = "https://cdn.example.test/logo.png",
        };

        remoteImageImporter
            .Setup(importer => importer.ImportAsync(
                It.Is<RemoteImageImportRequest>(request =>
                    request.Category == ImageCategory.Logo &&
                    !request.WithWatermark),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(importedImage);

        ImportRemoteImageCommandHandler handler = CreateHandler(remoteImageImporter);

        ApplicationResult<Image> result = await handler.HandleAsync(new ImportRemoteImageCommand(new RemoteImageImportRequest
        {
            SourceUrl = "https://cdn.example.test/logo.png",
            Category = ImageCategory.Logo,
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            WithWatermark = true,
            SetAsCurrent = false,
        }));

        Assert.True(result.IsSuccess);
        remoteImageImporter.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenLogoIsSetAsCurrent_ShouldSynchronizeLogo()
    {
        Mock<IRemoteImageImporter> remoteImageImporter = new Mock<IRemoteImageImporter>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        Image importedImage = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Logo,
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            SourceUrl = "https://cdn.example.test/logo.avif",
        };
        Image currentImage = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Logo,
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            IsCurrent = true,
            SourceUrl = "https://cdn.example.test/logo.avif",
        };
        Park park = new Park
        {
            Id = "park-1",
            Name = "Test Park",
            CurrentLogoImageId = "old-logo",
        };

        remoteImageImporter
            .Setup(importer => importer.ImportAsync(It.IsAny<RemoteImageImportRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(importedImage);

        imageRepository
            .Setup(repository => repository.SetCurrentAsync("image-1", ImageOwnerType.Park, "park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(currentImage);

        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        parkRepository
            .Setup(repository => repository.UpdateAsync(
                "park-1",
                It.Is<Park>(updatedPark => updatedPark.CurrentLogoImageId == "image-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, Park updatedPark, CancellationToken _) => updatedPark);

        ImportRemoteImageCommandHandler handler = new ImportRemoteImageCommandHandler(
            remoteImageImporter.Object,
            imageRepository.Object,
            parkRepository.Object,
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            userRepository.Object);

        ApplicationResult<Image> result = await handler.HandleAsync(new ImportRemoteImageCommand(new RemoteImageImportRequest
        {
            SourceUrl = "https://cdn.example.test/logo.avif",
            Category = ImageCategory.Logo,
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            WithWatermark = false,
            SetAsCurrent = true,
        }));

        Assert.True(result.IsSuccess);
        Assert.Equal("image-1", result.Value?.Id);
        Assert.Equal("image-1", park.CurrentLogoImageId);
        remoteImageImporter.VerifyAll();
        imageRepository.VerifyAll();
        parkRepository.VerifyAll();
        userRepository.VerifyNoOtherCalls();
    }

    private static ImportRemoteImageCommandHandler CreateHandler(Mock<IRemoteImageImporter> remoteImageImporter)
    {
        return new ImportRemoteImageCommandHandler(
            remoteImageImporter.Object,
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            Mock.Of<IUserRepository>(MockBehavior.Strict));
    }
}
