using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkItems;

public sealed class ParkItemReferenceValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EnsureParkExistsAsync_WhenParkIdIsBlank_ShouldReturnParkNotExistsWithoutCallingRepository(string? parkId)
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        ParkItemReferenceValidator validator = CreateValidator(parkRepository);

        ApplicationError? error = await validator.EnsureParkExistsAsync(parkId!, CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal("park.not-found", error.Code);
        parkRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnsureParkExistsAsync_WhenParkDoesNotExist_ShouldReturnParkNotExists()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository.Setup(item => item.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>())).ReturnsAsync((Park?)null);
        ParkItemReferenceValidator validator = CreateValidator(parkRepository);

        ApplicationError? error = await validator.EnsureParkExistsAsync(" park-1 ", CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal("park.not-found", error.Code);
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task EnsureParkExistsAsync_WhenParkExists_ShouldReturnNull()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository.Setup(item => item.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>())).ReturnsAsync(new Park { Id = "park-1" });
        ParkItemReferenceValidator validator = CreateValidator(parkRepository);

        ApplicationError? error = await validator.EnsureParkExistsAsync("park-1", CancellationToken.None);

        Assert.Null(error);
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task ValidateForWriteAsync_WhenParkItemIsNull_ShouldThrow()
    {
        ParkItemReferenceValidator validator = CreateValidator();

        await Assert.ThrowsAsync<ArgumentNullException>(() => validator.ValidateForWriteAsync(null!, CancellationToken.None));
    }

    [Fact]
    public async Task ValidateForWriteAsync_WhenZoneDoesNotExist_ShouldReturnParkZoneNotExists()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> zoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        parkRepository.Setup(item => item.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>())).ReturnsAsync(new Park { Id = "park-1" });
        zoneRepository.Setup(item => item.GetByIdAsync("zone-1", It.IsAny<CancellationToken>())).ReturnsAsync((ParkZone?)null);
        ParkItemReferenceValidator validator = CreateValidator(parkRepository, zoneRepository);
        ParkItem parkItem = new ParkItem { ParkId = "park-1", ZoneId = " zone-1 " };

        ApplicationError? error = await validator.ValidateForWriteAsync(parkItem, CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal("park-zone.not-found", error.Code);
    }

    [Fact]
    public async Task ValidateForWriteAsync_WhenZoneBelongsToAnotherPark_ShouldReturnParkZoneNotExists()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> zoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        parkRepository.Setup(item => item.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>())).ReturnsAsync(new Park { Id = "park-1" });
        zoneRepository.Setup(item => item.GetByIdAsync("zone-1", It.IsAny<CancellationToken>())).ReturnsAsync(new ParkZone { Id = "zone-1", ParkId = "other-park" });
        ParkItemReferenceValidator validator = CreateValidator(parkRepository, zoneRepository);
        ParkItem parkItem = new ParkItem { ParkId = "park-1", ZoneId = "zone-1" };

        ApplicationError? error = await validator.ValidateForWriteAsync(parkItem, CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal("park-zone.not-found", error.Code);
    }

    [Fact]
    public async Task ValidateForWriteAsync_WhenAttractionManufacturerDoesNotExist_ShouldReturnManufacturerNotExists()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> zoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        parkRepository.Setup(item => item.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>())).ReturnsAsync(new Park { Id = "park-1" });
        manufacturerRepository.Setup(item => item.GetByIdAsync("manufacturer-1", It.IsAny<CancellationToken>())).ReturnsAsync((AttractionManufacturer?)null);
        ParkItemReferenceValidator validator = CreateValidator(parkRepository, zoneRepository, manufacturerRepository);
        ParkItem parkItem = new ParkItem
        {
            ParkId = "park-1",
            Category = ParkItemCategory.Attraction,
            AttractionDetails = new AttractionDetails { ManufacturerId = " manufacturer-1 " },
        };

        ApplicationError? error = await validator.ValidateForWriteAsync(parkItem, CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal("attraction-manufacturer.not-found", error.Code);
    }

    [Fact]
    public async Task ValidateForWriteAsync_WhenReferencesAreValid_ShouldReturnNull()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> zoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        parkRepository.Setup(item => item.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>())).ReturnsAsync(new Park { Id = "park-1" });
        zoneRepository.Setup(item => item.GetByIdAsync("zone-1", It.IsAny<CancellationToken>())).ReturnsAsync(new ParkZone { Id = "zone-1", ParkId = "park-1" });
        manufacturerRepository.Setup(item => item.GetByIdAsync("manufacturer-1", It.IsAny<CancellationToken>())).ReturnsAsync(new AttractionManufacturer { Id = "manufacturer-1" });
        ParkItemReferenceValidator validator = CreateValidator(parkRepository, zoneRepository, manufacturerRepository);
        ParkItem parkItem = new ParkItem
        {
            ParkId = "park-1",
            ZoneId = " zone-1 ",
            Category = ParkItemCategory.Attraction,
            AttractionDetails = new AttractionDetails { ManufacturerId = " manufacturer-1 " },
        };

        ApplicationError? error = await validator.ValidateForWriteAsync(parkItem, CancellationToken.None);

        Assert.Null(error);
    }

    private static ParkItemReferenceValidator CreateValidator(
        Mock<IParkRepository>? parkRepository = null,
        Mock<IParkZoneRepository>? zoneRepository = null,
        Mock<IAttractionManufacturerRepository>? manufacturerRepository = null)
    {
        return new ParkItemReferenceValidator(
            parkRepository?.Object ?? Mock.Of<IParkRepository>(),
            zoneRepository?.Object ?? Mock.Of<IParkZoneRepository>(),
            manufacturerRepository?.Object ?? Mock.Of<IAttractionManufacturerRepository>());
    }
}
