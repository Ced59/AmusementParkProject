using System.Text;
using System.Text.Json;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Handlers;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Handlers;

public sealed class ExportStandaloneAttractionGraphJsonQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenStandaloneHasHistory_ShouldExportHistoryWithStandaloneOwner()
    {
        StandaloneAttraction attraction = new StandaloneAttraction
        {
            Id = "standalone-1",
            Name = "Pendolino",
            CountryCode = "AT",
            Type = ParkItemType.RollerCoaster,
            OperatorId = "operator-1",
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
            LegacyParkId = "legacy-park-1",
            LegacyParkItemId = "legacy-item-1",
            AttractionDetails = new AttractionDetails
            {
                ManufacturerId = "manufacturer-1",
                Status = ParkItemStatusNormalizer.Operating,
            },
        };
        HistoryEvent historyEvent = new HistoryEvent
        {
            Id = "history-1",
            Key = "pendolino-opening-2007",
            EntityType = HistoryEntityType.StandaloneAttraction,
            OwnerId = "standalone-1",
            Year = 2007,
            DatePrecision = HistoryDatePrecision.Year,
            EventType = ParkItemHistoryEventType.Opening.ToString(),
            IsMajor = true,
            IsVisible = true,
            Titles = new List<LocalizedText>
            {
                new LocalizedText("fr", "Ouverture de Pendolino"),
            },
            Article = new HistoryArticle
            {
                Slug = "ouverture-pendolino",
                IsPublished = true,
                Blocks = new List<HistoryArticleBlock>
                {
                    new HistoryArticleBlock
                    {
                        Id = "block-1",
                        Type = HistoryArticleBlockType.Paragraph,
                        SortOrder = 1,
                        Texts = new List<LocalizedText>
                        {
                            new LocalizedText("fr", "Le récit complet de l'ouverture."),
                        },
                    },
                },
                Sources = new List<HistorySourceReference>
                {
                    new HistorySourceReference
                    {
                        Label = "Source officielle",
                        Url = "https://example.com/history",
                    },
                },
            },
        };

        Mock<IStandaloneAttractionRepository> standaloneRepository = new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict);
        standaloneRepository
            .Setup(repository => repository.GetByIdAsync("standalone-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attraction);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(repository => repository.GetByOwnersAsync(
                ImageOwnerType.StandaloneAttraction,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "standalone-1" })),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Image>());
        Mock<IHistoryEventRepository> historyRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        historyRepository
            .Setup(repository => repository.GetOwnerTimelineAsync(
                HistoryEntityType.StandaloneAttraction,
                "standalone-1",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { historyEvent });
        ParkOpeningHoursSchedule openingHours = new ParkOpeningHoursSchedule
        {
            ParkId = "standalone-1",
            TimeZoneId = "Europe/Vienna",
            SourceUrl = "https://example.com/hours",
        };
        Mock<IStandaloneAttractionOpeningHoursRepository> openingHoursRepository =
            new Mock<IStandaloneAttractionOpeningHoursRepository>(MockBehavior.Strict);
        openingHoursRepository
            .Setup(repository => repository.GetByStandaloneAttractionIdAsync(
                "standalone-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(openingHours);
        ParkPricingEntity pricing = new ParkPricingEntity
        {
            ParkId = "standalone-1",
            CurrencyCode = "EUR",
            SourceUrl = "https://example.com/prices",
        };
        Mock<IStandaloneAttractionPricingRepository> pricingRepository =
            new Mock<IStandaloneAttractionPricingRepository>(MockBehavior.Strict);
        pricingRepository
            .Setup(repository => repository.GetByStandaloneAttractionIdAsync(
                "standalone-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(pricing);

        Mock<IParkOperatorRepository> operatorRepository = new(MockBehavior.Strict);
        operatorRepository.Setup(repository => repository.GetByIdAsync("operator-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ParkOperator { Id = "operator-1", Name = "Mountain Leisure",
                Description = new() { new("fr", "L'exploitant des installations de la station.") } });
        Mock<IAttractionManufacturerRepository> manufacturerRepository = new(MockBehavior.Strict);
        manufacturerRepository.Setup(repository => repository.GetByIdAsync("manufacturer-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AttractionManufacturer { Id = "manufacturer-1", Name = "Alpine Workshop", IsVisible = false,
                Biography = new() { new("fr", "L'atelier construit des luges de montagne.") } });
        imageRepository.Setup(repository => repository.GetByOwnersAsync(ImageOwnerType.ParkOperator,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "operator-1" })), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Image { Id = "operator-photo", OwnerType = ImageOwnerType.ParkOperator,
                OwnerId = "operator-1", IsPublished = false, AltTexts = new() { new("fr", "Le bâtiment de la station") } } });
        imageRepository.Setup(repository => repository.GetByOwnersAsync(ImageOwnerType.AttractionManufacturer,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "manufacturer-1" })), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Image>());

        ExportParkGraphJsonQueryHandler handler = new ExportParkGraphJsonQueryHandler(
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            operatorRepository.Object,
            manufacturerRepository.Object,
            imageRepository.Object,
            historyEventRepository: historyRepository.Object,
            standaloneAttractionRepository: standaloneRepository.Object,
            standaloneOpeningHoursRepository: openingHoursRepository.Object,
            standalonePricingRepository: pricingRepository.Object);

        ApplicationResult<ParkGraphJsonExportResult> result = await handler.HandleAsync(
            new ExportStandaloneAttractionGraphJsonQuery("standalone-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        using JsonDocument document = JsonDocument.Parse(Encoding.UTF8.GetString(result.Value!.Content));
        JsonElement exportedEvent = Assert.Single(document.RootElement.GetProperty("history").GetProperty("events").EnumerateArray().ToArray());
        Assert.Equal("StandaloneAttraction", exportedEvent.GetProperty("entityType").GetString());
        Assert.Equal("standaloneAttraction", exportedEvent.GetProperty("owner").GetString());
        Assert.Equal("standalone-1", exportedEvent.GetProperty("ownerId").GetString());
        Assert.Equal(2007, exportedEvent.GetProperty("year").GetInt32());
        Assert.Equal("Opening", exportedEvent.GetProperty("eventType").GetString());
        JsonElement article = exportedEvent.GetProperty("article");
        Assert.Equal("block-1", Assert.Single(article.GetProperty("blocks").EnumerateArray().ToArray()).GetProperty("id").GetString());
        Assert.Equal("https://example.com/history", Assert.Single(article.GetProperty("sources").EnumerateArray().ToArray()).GetProperty("url").GetString());
        Assert.False(document.RootElement.GetProperty("migration").GetProperty("retireLegacyPark").GetBoolean());
        Assert.False(document.RootElement.GetProperty("migration").GetProperty("retireLegacyParkItem").GetBoolean());
        Assert.Equal(JsonValueKind.Null, document.RootElement.GetProperty("standaloneAttraction").GetProperty("operatorKey").ValueKind);
        Assert.Equal(
            JsonValueKind.Null,
            document.RootElement.GetProperty("standaloneAttraction").GetProperty("attractionDetails").GetProperty("manufacturerKey").ValueKind);
        JsonElement exportedOpeningHours = document.RootElement.GetProperty("openingHours");
        Assert.Equal("standalone-1", exportedOpeningHours.GetProperty("standaloneAttractionId").GetString());
        Assert.False(exportedOpeningHours.TryGetProperty("parkId", out _));
        Assert.Equal("Europe/Vienna", exportedOpeningHours.GetProperty("timeZoneId").GetString());
        JsonElement exportedPricing = document.RootElement.GetProperty("pricing");
        Assert.Equal("standalone-1", exportedPricing.GetProperty("standaloneAttractionId").GetString());
        Assert.False(exportedPricing.TryGetProperty("parkId", out _));
        Assert.Equal("EUR", exportedPricing.GetProperty("currencyCode").GetString());
        JsonElement references = document.RootElement.GetProperty("references");
        JsonElement exportedOperator = Assert.Single(references.GetProperty("operators").EnumerateArray());
        Assert.Equal("operator-1", exportedOperator.GetProperty("id").GetString());
        Assert.Equal("L'exploitant des installations de la station.",
            Assert.Single(exportedOperator.GetProperty("description").EnumerateArray()).GetProperty("value").GetString());
        JsonElement exportedManufacturer = Assert.Single(references.GetProperty("manufacturers").EnumerateArray());
        Assert.Equal("manufacturer-1", exportedManufacturer.GetProperty("id").GetString());
        Assert.False(exportedManufacturer.GetProperty("isVisible").GetBoolean());
        Assert.Single(exportedManufacturer.GetProperty("biography").EnumerateArray());
        JsonElement exportedReferenceImage = Assert.Single(document.RootElement.GetProperty("images").EnumerateArray());
        Assert.Equal("ParkOperator", exportedReferenceImage.GetProperty("ownerType").GetString());
        Assert.Equal("operator-1", exportedReferenceImage.GetProperty("ownerId").GetString());
        Assert.Equal("operator:operator-1", exportedReferenceImage.GetProperty("ownerKey").GetString());
        Assert.False(exportedReferenceImage.GetProperty("isPublished").GetBoolean());
        operatorRepository.VerifyAll();
        manufacturerRepository.VerifyAll();

        standaloneRepository.VerifyAll();
        imageRepository.VerifyAll();
        historyRepository.VerifyAll();
        openingHoursRepository.VerifyAll();
        pricingRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenStandaloneIsNotOperating_ShouldOmitVisitorInformation()
    {
        StandaloneAttraction attraction = new StandaloneAttraction
        {
            Id = "standalone-closed",
            Name = "Ancienne luge sur rail",
            CountryCode = "FR",
            Type = ParkItemType.RollerCoaster,
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
            AttractionDetails = new AttractionDetails
            {
                Status = ParkItemStatusNormalizer.ClosedDefinitively,
            },
        };
        Mock<IStandaloneAttractionRepository> standaloneRepository =
            new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict);
        standaloneRepository
            .Setup(repository => repository.GetByIdAsync(
                "standalone-closed",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(attraction);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(repository => repository.GetByOwnersAsync(
                ImageOwnerType.StandaloneAttraction,
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "standalone-closed" })),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Image>());
        Mock<IStandaloneAttractionOpeningHoursRepository> openingHoursRepository =
            new Mock<IStandaloneAttractionOpeningHoursRepository>(MockBehavior.Strict);
        Mock<IStandaloneAttractionPricingRepository> pricingRepository =
            new Mock<IStandaloneAttractionPricingRepository>(MockBehavior.Strict);
        ExportParkGraphJsonQueryHandler handler = new ExportParkGraphJsonQueryHandler(
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            imageRepository.Object,
            standaloneAttractionRepository: standaloneRepository.Object,
            standaloneOpeningHoursRepository: openingHoursRepository.Object,
            standalonePricingRepository: pricingRepository.Object);

        ApplicationResult<ParkGraphJsonExportResult> result = await handler.HandleAsync(
            new ExportStandaloneAttractionGraphJsonQuery("standalone-closed"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        using JsonDocument document = JsonDocument.Parse(Encoding.UTF8.GetString(result.Value!.Content));
        Assert.False(document.RootElement.TryGetProperty("openingHours", out _));
        Assert.False(document.RootElement.TryGetProperty("pricing", out _));
        standaloneRepository.VerifyAll();
        imageRepository.VerifyAll();
        openingHoursRepository.VerifyNoOtherCalls();
        pricingRepository.VerifyNoOtherCalls();
    }
}
