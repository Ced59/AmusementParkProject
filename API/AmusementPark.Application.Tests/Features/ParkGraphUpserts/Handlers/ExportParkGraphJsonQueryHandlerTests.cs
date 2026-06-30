using System.Text.Json;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Handlers;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Handlers;

public sealed class ExportParkGraphJsonQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenParkDoesNotExist_ShouldReturnNotFound()
    {
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("missing", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Park?)null);

        ExportParkGraphJsonQueryHandler handler = new ExportParkGraphJsonQueryHandler(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict));

        ApplicationResult<ParkGraphJsonExportResult> result = await handler.HandleAsync(
            new ExportParkGraphJsonQuery("missing"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorType.NotFound, result.Errors.Single().Type);
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenParkExists_ShouldExportCurrentGraphAsUpsertJson()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Export Park",
            CountryCode = "FR",
            Type = ParkType.ThemePark,
            FounderId = "founder-1",
            OperatorId = "operator-1",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
            WebsiteUrl = "https://example.test",
            City = "Paris",
            OpeningDate = new DateTime(1987, 5, 20),
            ClosingDate = new DateTime(1991, 10, 20),
            OpeningDateText = "1987-05-20",
            ClosingDateText = "1991-10-20",
        };
        park.SetPosition(48.85, 2.35);

        ParkZone zone = new ParkZone
        {
            Id = "zone-1",
            ParkId = "park-1",
            Name = "Main Zone",
            IsVisible = true,
            SortOrder = 2,
        };

        ParkItem item = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            ZoneId = "zone-1",
            Name = "Drop tower",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.DropTower,
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
            AttractionDetails = new AttractionDetails
            {
                ManufacturerId = "manufacturer-1",
                Model = "Model A",
                IsLaunched = true,
            },
        };

        ParkFounder founder = new ParkFounder
        {
            Id = "founder-1",
            Name = "Founder",
        };

        ParkOperator parkOperator = new ParkOperator
        {
            Id = "operator-1",
            Name = "Operator",
            AdminReviewStatus = AdminReviewStatus.Validated,
        };

        AttractionManufacturer manufacturer = new AttractionManufacturer
        {
            Id = "manufacturer-1",
            Name = "Manufacturer",
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };

        Image parkImage = new Image
        {
            Id = "image-park-1",
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Category = ImageCategory.Park,
            IsCurrent = true,
            IsPublished = true,
            OriginalFileName = "park.jpg",
            ContentType = "image/jpeg",
            Width = 1200,
            Height = 800,
            SizeInBytes = 2048,
            SourceUrl = "https://source.example.test/park.jpg",
        };

        Image itemImage = new Image
        {
            Id = "image-item-1",
            OwnerType = ImageOwnerType.ParkItem,
            OwnerId = "item-1",
            Category = ImageCategory.ParkItem,
            IsPublished = false,
            OriginalFileName = "item.jpg",
        };

        Image manufacturerImage = new Image
        {
            Id = "image-manufacturer-1",
            OwnerType = ImageOwnerType.AttractionManufacturer,
            OwnerId = "manufacturer-1",
            Category = ImageCategory.Manufacturer,
            IsPublished = true,
            OriginalFileName = "manufacturer.png",
            SourceUrl = "https://source.example.test/manufacturer.png",
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        Mock<IParkZoneRepository> zoneRepository = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        zoneRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { zone });

        Mock<IParkItemRepository> itemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        itemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });

        Mock<IParkFounderRepository> founderRepository = new Mock<IParkFounderRepository>(MockBehavior.Strict);
        founderRepository
            .Setup(repository => repository.GetByIdAsync("founder-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(founder);

        Mock<IParkOperatorRepository> operatorRepository = new Mock<IParkOperatorRepository>(MockBehavior.Strict);
        operatorRepository
            .Setup(repository => repository.GetByIdAsync("operator-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(parkOperator);

        Mock<IAttractionManufacturerRepository> manufacturerRepository = new Mock<IAttractionManufacturerRepository>(MockBehavior.Strict);
        manufacturerRepository
            .Setup(repository => repository.GetByIdAsync("manufacturer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(manufacturer);

        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(repository => repository.GetByOwnersAsync(ImageOwnerType.Park, It.Is<IReadOnlyCollection<string>>(ownerIds => ownerIds.Contains("park-1")), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { parkImage });
        imageRepository
            .Setup(repository => repository.GetByOwnersAsync(ImageOwnerType.ParkItem, It.Is<IReadOnlyCollection<string>>(ownerIds => ownerIds.Contains("item-1")), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { itemImage });
        imageRepository
            .Setup(repository => repository.GetByOwnersAsync(ImageOwnerType.ParkFounder, It.Is<IReadOnlyCollection<string>>(ownerIds => ownerIds.Contains("founder-1")), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Image>());
        imageRepository
            .Setup(repository => repository.GetByOwnersAsync(ImageOwnerType.ParkOperator, It.Is<IReadOnlyCollection<string>>(ownerIds => ownerIds.Contains("operator-1")), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Image>());
        imageRepository
            .Setup(repository => repository.GetByOwnersAsync(ImageOwnerType.AttractionManufacturer, It.Is<IReadOnlyCollection<string>>(ownerIds => ownerIds.Contains("manufacturer-1")), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { manufacturerImage });

        ExportParkGraphJsonQueryHandler handler = new ExportParkGraphJsonQueryHandler(
            parkRepository.Object,
            zoneRepository.Object,
            itemRepository.Object,
            founderRepository.Object,
            operatorRepository.Object,
            manufacturerRepository.Object,
            imageRepository.Object);

        ApplicationResult<ParkGraphJsonExportResult> result = await handler.HandleAsync(
            new ExportParkGraphJsonQuery("park-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.EndsWith("-park-graph.json", result.Value.FileName);

        using JsonDocument document = JsonDocument.Parse(result.Value.Json);
        JsonElement root = document.RootElement;

        Assert.Equal("AmusementParkParkGraphUpsert", root.GetProperty("documentType").GetString());
        Assert.Equal("park-1", root.GetProperty("identity").GetProperty("parkId").GetString());
        Assert.Equal("Export Park", root.GetProperty("park").GetProperty("name").GetString());
        Assert.Equal("Validated", root.GetProperty("park").GetProperty("adminReviewStatus").GetString());
        Assert.Equal("1987-05-20T00:00:00", root.GetProperty("park").GetProperty("openingDate").GetString());
        Assert.Equal("1991-10-20T00:00:00", root.GetProperty("park").GetProperty("closingDate").GetString());
        Assert.Equal("1987-05-20", root.GetProperty("park").GetProperty("openingDateText").GetString());
        Assert.Equal("1991-10-20", root.GetProperty("park").GetProperty("closingDateText").GetString());
        Assert.Equal("zone-1", root.GetProperty("zones")[0].GetProperty("key").GetString());
        Assert.Equal("DropTower", root.GetProperty("items")[0].GetProperty("type").GetString());
        Assert.Equal("zone-1", root.GetProperty("items")[0].GetProperty("zoneKey").GetString());
        Assert.Equal("manufacturer-1", root.GetProperty("items")[0].GetProperty("attractionDetails").GetProperty("manufacturerKey").GetString());
        Assert.Equal("Manufacturer", root.GetProperty("references").GetProperty("manufacturers")[0].GetProperty("name").GetString());
        Assert.False(root.GetProperty("references").GetProperty("manufacturers")[0].GetProperty("isVisible").GetBoolean());
        JsonElement parkExportImage = root.GetProperty("images").EnumerateArray().Single(image => image.GetProperty("imageId").GetString() == "image-park-1");
        JsonElement itemExportImage = root.GetProperty("images").EnumerateArray().Single(image => image.GetProperty("imageId").GetString() == "image-item-1");
        JsonElement manufacturerExportImage = root.GetProperty("images").EnumerateArray().Single(image => image.GetProperty("imageId").GetString() == "image-manufacturer-1");
        Assert.Equal("park", parkExportImage.GetProperty("ownerKey").GetString());
        Assert.Equal("https://source.example.test/park.jpg", parkExportImage.GetProperty("sourceUrl").GetString());
        Assert.Equal("/images/image-park-1", parkExportImage.GetProperty("internalUrl").GetString());
        Assert.False(parkExportImage.GetProperty("withWatermark").GetBoolean());
        Assert.Equal("item-1", itemExportImage.GetProperty("ownerKey").GetString());
        Assert.Equal("manufacturer:manufacturer-1", manufacturerExportImage.GetProperty("ownerKey").GetString());
        Assert.Equal("https://source.example.test/manufacturer.png", manufacturerExportImage.GetProperty("sourceUrl").GetString());
        Assert.Equal("/images/image-manufacturer-1", manufacturerExportImage.GetProperty("internalUrl").GetString());
        Assert.False(manufacturerExportImage.GetProperty("withWatermark").GetBoolean());

        parkRepository.VerifyAll();
        zoneRepository.VerifyAll();
        itemRepository.VerifyAll();
        founderRepository.VerifyAll();
        operatorRepository.VerifyAll();
        manufacturerRepository.VerifyAll();
        imageRepository.VerifyAll();
    }
}
