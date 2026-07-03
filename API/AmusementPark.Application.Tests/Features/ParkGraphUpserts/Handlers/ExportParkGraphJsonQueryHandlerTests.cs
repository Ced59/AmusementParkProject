using System.Text.Json;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Handlers;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
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
    public async Task HandleAsync_WhenNoSectionIsSelected_ShouldExportIdentityOnly()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Identity Park",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        ExportParkGraphJsonQueryHandler handler = new ExportParkGraphJsonQueryHandler(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict));

        ApplicationResult<ParkGraphJsonExportResult> result = await handler.HandleAsync(
            new ExportParkGraphJsonQuery("park-1", Array.Empty<ParkGraphExportSection>()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        using JsonDocument document = JsonDocument.Parse(result.Value.Json);
        JsonElement root = document.RootElement;

        Assert.Equal("park-1", root.GetProperty("identity").GetProperty("parkId").GetString());
        Assert.Equal("Identity Park", root.GetProperty("identity").GetProperty("name").GetString());
        Assert.Equal("FR", root.GetProperty("identity").GetProperty("countryCode").GetString());
        Assert.False(root.TryGetProperty("park", out _));
        Assert.False(root.TryGetProperty("items", out _));
        Assert.False(root.TryGetProperty("zones", out _));
        Assert.False(root.TryGetProperty("references", out _));
        Assert.True(root.TryGetProperty("metadata", out _));
        parkRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenSelectedParkFieldIsEmpty_ShouldKeepNullPropertyInExport()
    {
        Park park = new Park
        {
            Id = "park-1",
            Name = "Null Field Park",
            CountryCode = "FR",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };

        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        ExportParkGraphJsonQueryHandler handler = new ExportParkGraphJsonQueryHandler(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict));

        ApplicationResult<ParkGraphJsonExportResult> result = await handler.HandleAsync(
            new ExportParkGraphJsonQuery("park-1", new[] { ParkGraphExportSection.ParkLocation }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);

        using JsonDocument document = JsonDocument.Parse(result.Value.Json);
        JsonElement parkPatch = document.RootElement.GetProperty("park");

        Assert.Equal("FR", parkPatch.GetProperty("countryCode").GetString());
        Assert.Equal(JsonValueKind.Null, parkPatch.GetProperty("street").ValueKind);
        Assert.Equal(JsonValueKind.Null, parkPatch.GetProperty("city").ValueKind);
        Assert.Equal(JsonValueKind.Null, parkPatch.GetProperty("postalCode").ValueKind);
        Assert.Equal(JsonValueKind.Null, parkPatch.GetProperty("latitude").ValueKind);
        Assert.Equal(JsonValueKind.Null, parkPatch.GetProperty("longitude").ValueKind);
        Assert.False(parkPatch.TryGetProperty("name", out _));
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

        HistoryEvent parkHistoryEvent = new HistoryEvent
        {
            Id = "history-park-1",
            Key = "park-opening",
            EntityType = HistoryEntityType.Park,
            OwnerId = "park-1",
            ParkId = "park-1",
            Year = 1987,
            Month = 5,
            Day = 20,
            DatePrecision = HistoryDatePrecision.Day,
            EventType = ParkHistoryEventType.Opening.ToString(),
            IsMajor = true,
            IsVisible = true,
            Slug = "park-opening",
            MainImageId = "image-park-1",
            Titles = new List<LocalizedText>
            {
                new LocalizedText("fr", "Ouverture du parc"),
            },
            Summaries = new List<LocalizedText>
            {
                new LocalizedText("fr", "Le parc ouvre au public avec ses premieres attractions."),
            },
            Sources = new List<HistorySourceReference>
            {
                new HistorySourceReference
                {
                    Label = "Archive officielle",
                    Url = "https://source.example.test/opening",
                    AccessedAt = "2026-06-30",
                },
            },
            Article = new HistoryArticle
            {
                Slug = "park-opening-article",
                Titles = new List<LocalizedText>
                {
                    new LocalizedText("fr", "Le parc ouvre ses portes"),
                },
                Subtitles = new List<LocalizedText>
                {
                    new LocalizedText("fr", "Une nouvelle destination de loisirs arrive en Ile-de-France."),
                },
                Summaries = new List<LocalizedText>
                {
                    new LocalizedText("fr", "L'ouverture marque le lancement du parc et de ses attractions."),
                },
                MainImageId = "image-park-1",
                Blocks = new List<HistoryArticleBlock>
                {
                    new HistoryArticleBlock
                    {
                        Id = "block-1",
                        Type = HistoryArticleBlockType.Paragraph,
                        SortOrder = 1,
                        Texts = new List<LocalizedText>
                        {
                            new LocalizedText("fr", "Le premier jour donne le ton du parc et de son offre familiale."),
                        },
                        ImageIds = new List<string>
                        {
                            "image-park-1",
                        },
                        Captions = new List<LocalizedText>
                        {
                            new LocalizedText("fr", "Vue du parc au moment de son ouverture."),
                        },
                    },
                },
                Sources = new List<HistorySourceReference>
                {
                    new HistorySourceReference
                    {
                        Label = "Dossier historique",
                        Url = "https://source.example.test/opening-article",
                        AccessedAt = "2026-06-30",
                    },
                },
                IsPublished = true,
            },
        };

        HistoryEvent itemHistoryEvent = new HistoryEvent
        {
            Id = "history-item-1",
            Key = "item-opening",
            EntityType = HistoryEntityType.ParkItem,
            OwnerId = "item-1",
            ParkItemId = "item-1",
            ContextParkId = "park-1",
            Year = 1988,
            DatePrecision = HistoryDatePrecision.Year,
            EventType = ParkItemHistoryEventType.Opening.ToString(),
            IsMajor = false,
            IsVisible = true,
            Titles = new List<LocalizedText>
            {
                new LocalizedText("fr", "Drop tower ouvre au public"),
            },
            Summaries = new List<LocalizedText>
            {
                new LocalizedText("fr", "L'attraction rejoint l'offre du parc."),
            },
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
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Contains("manufacturer-1")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { manufacturer });

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

        Mock<IHistoryEventRepository> historyEventRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        historyEventRepository
            .Setup(repository => repository.GetParkTimelineAsync(
                "park-1",
                true,
                true,
                It.Is<IReadOnlyCollection<string>>(ownerIds => ownerIds.Contains("item-1")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { parkHistoryEvent, itemHistoryEvent });

        ExportParkGraphJsonQueryHandler handler = new ExportParkGraphJsonQueryHandler(
            parkRepository.Object,
            zoneRepository.Object,
            itemRepository.Object,
            founderRepository.Object,
            operatorRepository.Object,
            manufacturerRepository.Object,
            imageRepository.Object,
            null,
            historyEventRepository.Object);

        ApplicationResult<ParkGraphJsonExportResult> result = await handler.HandleAsync(
            new ExportParkGraphJsonQuery("park-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.EndsWith("-park-graph.json", result.Value.FileName);

        using JsonDocument document = JsonDocument.Parse(result.Value.Json);
        JsonElement root = document.RootElement;

        Assert.Equal("AmusementParkParkGraphUpsert", root.GetProperty("documentType").GetString());
        Assert.Equal("2026-06-30", root.GetProperty("schemaVersion").GetString());
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
        JsonElement historyEvents = root.GetProperty("history").GetProperty("events");
        Assert.Equal(2, historyEvents.GetArrayLength());
        JsonElement parkExportEvent = historyEvents.EnumerateArray().Single(historyEvent => historyEvent.GetProperty("key").GetString() == "park-opening");
        Assert.Equal("Park", parkExportEvent.GetProperty("entityType").GetString());
        Assert.Equal("park", parkExportEvent.GetProperty("owner").GetString());
        Assert.Equal(string.Empty, parkExportEvent.GetProperty("ownerId").GetString());
        Assert.Equal(JsonValueKind.Null, parkExportEvent.GetProperty("parkId").ValueKind);
        Assert.Equal("Opening", parkExportEvent.GetProperty("eventType").GetString());
        Assert.Equal(1987, parkExportEvent.GetProperty("year").GetInt32());
        Assert.Equal(5, parkExportEvent.GetProperty("month").GetInt32());
        Assert.Equal(20, parkExportEvent.GetProperty("day").GetInt32());
        Assert.Equal("Day", parkExportEvent.GetProperty("datePrecision").GetString());
        Assert.Equal("https://source.example.test/opening", parkExportEvent.GetProperty("sources")[0].GetProperty("url").GetString());
        JsonElement parkExportArticle = parkExportEvent.GetProperty("article");
        Assert.True(parkExportArticle.GetProperty("isPublished").GetBoolean());
        Assert.Equal("park-opening-article", parkExportArticle.GetProperty("slug").GetString());
        Assert.Equal("Paragraph", parkExportArticle.GetProperty("blocks")[0].GetProperty("type").GetString());
        Assert.Equal("image-park-1", parkExportArticle.GetProperty("blocks")[0].GetProperty("imageIds")[0].GetString());
        Assert.Equal("https://source.example.test/opening-article", parkExportArticle.GetProperty("sources")[0].GetProperty("url").GetString());
        JsonElement itemExportEvent = historyEvents.EnumerateArray().Single(historyEvent => historyEvent.GetProperty("key").GetString() == "item-opening");
        Assert.Equal("ParkItem", itemExportEvent.GetProperty("entityType").GetString());
        Assert.Equal("parkItem", itemExportEvent.GetProperty("owner").GetString());
        Assert.Equal(string.Empty, itemExportEvent.GetProperty("ownerId").GetString());
        Assert.Equal(JsonValueKind.Null, itemExportEvent.GetProperty("parkItemId").ValueKind);
        Assert.Equal("item-1", itemExportEvent.GetProperty("itemKey").GetString());
        Assert.Equal("item-1", itemExportEvent.GetProperty("parkItemKey").GetString());
        Assert.Equal(JsonValueKind.Null, itemExportEvent.GetProperty("contextParkId").ValueKind);
        Assert.Equal("Year", itemExportEvent.GetProperty("datePrecision").GetString());

        parkRepository.VerifyAll();
        zoneRepository.VerifyAll();
        itemRepository.VerifyAll();
        founderRepository.VerifyAll();
        operatorRepository.VerifyAll();
        manufacturerRepository.VerifyAll();
        imageRepository.VerifyAll();
        historyEventRepository.VerifyAll();
    }
}
