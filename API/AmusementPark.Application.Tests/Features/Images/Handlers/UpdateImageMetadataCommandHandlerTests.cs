using AmusementPark.Application.Common.Contracts;
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

public sealed class UpdateImageMetadataCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCurrentParkLogoLeavesLogoCategory_ShouldClearParkCurrentLogo()
    {
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);

        Image existing = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Logo,
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            IsCurrent = true,
            IsPublished = true,
        };

        Image updated = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Park,
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            IsCurrent = false,
            IsPublished = true,
        };

        Park park = new Park
        {
            Id = "park-1",
            Name = "Test Park",
            CurrentLogoImageId = "image-1",
        };

        imageRepository
            .Setup(repository => repository.GetByIdAsync("image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        imageRepository
            .Setup(repository => repository.UpdateMetadataAsync(
                "image-1",
                It.Is<ImageMetadataUpdate>(metadata =>
                    metadata.Category == ImageCategory.Park &&
                    metadata.OwnerType == ImageOwnerType.Park &&
                    metadata.OwnerId == "park-1" &&
                    metadata.IsCurrent == false),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        imageRepository
            .Setup(repository => repository.GetCurrentByOwnerAsync(ImageOwnerType.Park, "park-1", ImageCategory.Logo, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Image?)null);

        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        parkRepository
            .Setup(repository => repository.UpdateAsync(
                "park-1",
                It.Is<Park>(value => value.CurrentLogoImageId == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string parkId, Park value, CancellationToken cancellationToken) => value);

        UpdateImageMetadataCommandHandler handler = new UpdateImageMetadataCommandHandler(
            imageRepository.Object,
            parkRepository.Object,
            manufacturerRepository.Object,
            searchProjectionWriter.Object,
            userRepository.Object);

        ApplicationResult<Image> result = await handler.HandleAsync(new UpdateImageMetadataCommand(" image-1 ", new ImageMetadataUpdate
        {
            Category = ImageCategory.Park,
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Description = existing.Description,
            GeoLocation = null,
            AltTexts = Array.Empty<LocalizedTextValue>(),
            Captions = Array.Empty<LocalizedTextValue>(),
            Credits = Array.Empty<LocalizedTextValue>(),
            TagIds = Array.Empty<string>(),
            IsPublished = true,
            SourceUrl = null,
        }));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value?.IsCurrent);
        imageRepository.VerifyAll();
        parkRepository.VerifyAll();
        manufacturerRepository.VerifyNoOtherCalls();
        searchProjectionWriter.VerifyNoOtherCalls();
        userRepository.VerifyNoOtherCalls();
    }
}
