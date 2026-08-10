using System.Text.Json;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Handlers;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Queries;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Services;

public sealed class ParkGraphPricingTests
{
    [Fact]
    public async Task PreviewAsync_WhenPricingIsValid_ShouldReportChangesWithoutSaving()
    {
        Park park = CreateOperatingPark();
        Mock<IParkPricingRepository> pricingRepository = new(MockBehavior.Strict);
        pricingRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkPricingEntity?)null);
        ProcessorContext context = CreateProcessorContext(park, pricingRepository, apply: false);
        ParkGraphUpsertRequest request = CreateRequest(CreatePricingDocumentJson());

        ApplicationResult<ParkGraphUpsertResult> result = await context.Processor.PreviewAsync(
            request,
            "user-1",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.CanApply);
        Assert.Empty(result.Value.Errors);
        ParkGraphUpsertChange change = Assert.Single(
            result.Value.Changes,
            static candidate => candidate.EntityType == "ParkPricing");
        Assert.Equal("Created", change.ChangeType);
        Assert.Contains(change.Fields, static field => field.Field == "pricing.admissionOffers");
        pricingRepository.Verify(
            repository => repository.UpsertAsync(It.IsAny<ParkPricingEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
        context.VerifyAll();
        pricingRepository.VerifyAll();
    }

    [Theory]
    [InlineData(ParkStatus.Planned)]
    [InlineData(ParkStatus.UnderConstruction)]
    [InlineData(ParkStatus.TemporarilyClosed)]
    [InlineData(ParkStatus.ClosedDefinitively)]
    [InlineData(ParkStatus.Cancelled)]
    public async Task PreviewAsync_WhenParkIsNotOperating_ShouldRejectPricing(ParkStatus status)
    {
        Park park = CreateOperatingPark();
        park.Status = status;
        Mock<IParkPricingRepository> pricingRepository = new(MockBehavior.Strict);
        ProcessorContext context = CreateProcessorContext(park, pricingRepository, apply: false);

        ApplicationResult<ParkGraphUpsertResult> result = await context.Processor.PreviewAsync(
            CreateRequest(CreatePricingDocumentJson()),
            "user-1",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.CanApply);
        Assert.Contains(result.Value.Errors, static error => error.Contains("pricing est réservé", StringComparison.Ordinal));
        pricingRepository.Verify(
            repository => repository.GetByParkIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        context.VerifyAll();
    }

    [Fact]
    public async Task ExportApplyExport_ShouldRoundTripPricingWithoutFunctionalLoss()
    {
        Park park = CreateOperatingPark();
        ParkPricingEntity sourcePricing = CreatePricing();
        string firstExport = await ExportPricingAsync(park, sourcePricing);
        ParkPricingEntity? savedPricing = null;

        Mock<IParkPricingRepository> importPricingRepository = new(MockBehavior.Strict);
        importPricingRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ParkPricingEntity?)null);
        importPricingRepository
            .Setup(repository => repository.UpsertAsync(It.IsAny<ParkPricingEntity>(), It.IsAny<CancellationToken>()))
            .Callback<ParkPricingEntity, CancellationToken>((pricing, _) => savedPricing = pricing)
            .ReturnsAsync((ParkPricingEntity pricing, CancellationToken _) => pricing);
        ProcessorContext context = CreateProcessorContext(park, importPricingRepository, apply: true);

        ApplicationResult<ParkGraphUpsertResult> applyResult = await context.Processor.ApplyAsync(
            CreateRequest(firstExport),
            "user-1",
            CancellationToken.None);

        Assert.True(applyResult.IsSuccess);
        Assert.NotNull(applyResult.Value);
        Assert.Empty(applyResult.Value.Errors);
        Assert.NotNull(savedPricing);
        ParkPricingSnapshot savedSnapshot = Assert.Single(savedPricing.HistoricalSnapshots);
        Assert.Equal(2024, savedSnapshot.Year);
        Assert.Equal("HRK", savedSnapshot.CurrencyCode);
        Assert.Equal("adult-high-season", Assert.Single(savedSnapshot.AdmissionOffers).Code);

        string secondExport = await ExportPricingAsync(park, savedPricing);
        using JsonDocument firstDocument = JsonDocument.Parse(firstExport);
        using JsonDocument secondDocument = JsonDocument.Parse(secondExport);

        Assert.Equal(
            firstDocument.RootElement.GetProperty("pricing").GetRawText(),
            secondDocument.RootElement.GetProperty("pricing").GetRawText());
        context.VerifyAll();
        importPricingRepository.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenPricingHistoryIsOmitted_ShouldPreserveExistingSnapshots()
    {
        Park park = CreateOperatingPark();
        ParkPricingEntity existingPricing = CreatePricing();
        ParkPricingEntity? savedPricing = null;
        Mock<IParkPricingRepository> pricingRepository = new(MockBehavior.Strict);
        pricingRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPricing);
        pricingRepository
            .Setup(repository => repository.UpsertAsync(It.IsAny<ParkPricingEntity>(), It.IsAny<CancellationToken>()))
            .Callback<ParkPricingEntity, CancellationToken>((pricing, _) => savedPricing = pricing)
            .ReturnsAsync((ParkPricingEntity pricing, CancellationToken _) => pricing);
        ProcessorContext context = CreateProcessorContext(park, pricingRepository, apply: true);

        ApplicationResult<ParkGraphUpsertResult> result = await context.Processor.ApplyAsync(
            CreateRequest(CreatePricingDocumentJson()),
            "user-1",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(savedPricing);
        Assert.Equal(2024, Assert.Single(savedPricing.HistoricalSnapshots).Year);
        context.VerifyAll();
        pricingRepository.VerifyAll();
    }

    [Fact]
    public async Task ApplyAsync_WhenPricingHistoryIsExplicitlyEmpty_ShouldClearSnapshots()
    {
        Park park = CreateOperatingPark();
        ParkPricingEntity existingPricing = CreatePricing();
        ParkPricingEntity? savedPricing = null;
        Mock<IParkPricingRepository> pricingRepository = new(MockBehavior.Strict);
        pricingRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingPricing);
        pricingRepository
            .Setup(repository => repository.UpsertAsync(It.IsAny<ParkPricingEntity>(), It.IsAny<CancellationToken>()))
            .Callback<ParkPricingEntity, CancellationToken>((pricing, _) => savedPricing = pricing)
            .ReturnsAsync((ParkPricingEntity pricing, CancellationToken _) => pricing);
        ProcessorContext context = CreateProcessorContext(park, pricingRepository, apply: true);
        string json = CreatePricingDocumentJson().Replace(
            "\"parkingOffers\": []",
            "\"parkingOffers\": [], \"historicalSnapshots\": []",
            StringComparison.Ordinal);

        ApplicationResult<ParkGraphUpsertResult> result = await context.Processor.ApplyAsync(
            CreateRequest(json),
            "user-1",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(savedPricing);
        Assert.Empty(savedPricing.HistoricalSnapshots);
        context.VerifyAll();
        pricingRepository.VerifyAll();
    }

    [Fact]
    public async Task ExportWithoutPricing_Preview_ShouldTreatNullPricingAsAbsent()
    {
        Park park = CreateOperatingPark();
        string exportedJson = await ExportPricingAsync(park, null);
        using JsonDocument exportedDocument = JsonDocument.Parse(exportedJson);
        Assert.Equal(JsonValueKind.Null, exportedDocument.RootElement.GetProperty("pricing").ValueKind);

        Mock<IParkPricingRepository> importPricingRepository = new(MockBehavior.Strict);
        ProcessorContext context = CreateProcessorContext(park, importPricingRepository, apply: false);

        ApplicationResult<ParkGraphUpsertResult> result = await context.Processor.PreviewAsync(
            CreateRequest(exportedJson),
            "user-1",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.CanApply);
        Assert.Empty(result.Value.Errors);
        Assert.DoesNotContain(result.Value.Changes, static change => change.EntityType == "ParkPricing");
        context.VerifyAll();
        importPricingRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExportClosedParkWithRetainedPricing_Preview_ShouldTreatPricingAsAbsent()
    {
        Park park = CreateOperatingPark();
        park.Status = ParkStatus.TemporarilyClosed;
        string exportedJson = await ExportPricingAsync(park, CreatePricing());
        using JsonDocument exportedDocument = JsonDocument.Parse(exportedJson);
        Assert.Equal(JsonValueKind.Null, exportedDocument.RootElement.GetProperty("pricing").ValueKind);

        Mock<IParkPricingRepository> importPricingRepository = new(MockBehavior.Strict);
        ProcessorContext context = CreateProcessorContext(park, importPricingRepository, apply: false);

        ApplicationResult<ParkGraphUpsertResult> result = await context.Processor.PreviewAsync(
            CreateRequest(exportedJson),
            "user-1",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value.CanApply);
        Assert.Empty(result.Value.Errors);
        Assert.DoesNotContain(result.Value.Changes, static change => change.EntityType == "ParkPricing");
        context.VerifyAll();
        importPricingRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PreviewAsync_WhenPricingIsNotAnObject_ShouldRejectTheDocument()
    {
        Park park = CreateOperatingPark();
        Mock<IParkPricingRepository> pricingRepository = new(MockBehavior.Strict);
        ProcessorContext context = CreateProcessorContext(park, pricingRepository, apply: false);

        ApplicationResult<ParkGraphUpsertResult> result = await context.Processor.PreviewAsync(
            CreateRequest("""{ "mode": "merge", "pricing": 42 }"""),
            "user-1",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value.CanApply);
        Assert.Contains(result.Value.Errors, static error => error.Contains("pricing doit", StringComparison.Ordinal));
        context.VerifyAll();
        pricingRepository.VerifyNoOtherCalls();
    }

    private static async Task<string> ExportPricingAsync(Park park, ParkPricingEntity? pricing)
    {
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        Mock<IParkPricingRepository> pricingRepository = new(MockBehavior.Strict);
        if (park.Status.IsOpenToVisitors())
        {
            pricingRepository
                .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(pricing);
        }
        ExportParkGraphJsonQueryHandler handler = new(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            pricingRepository: pricingRepository.Object);

        ApplicationResult<ParkGraphJsonExportResult> result = await handler.HandleAsync(
            new ExportParkGraphJsonQuery("park-1", new[] { ParkGraphExportSection.Pricing }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        parkRepository.VerifyAll();
        pricingRepository.VerifyAll();
        if (!park.Status.IsOpenToVisitors())
        {
            pricingRepository.Verify(
                repository => repository.GetByParkIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        return result.Value.Json;
    }

    private static ProcessorContext CreateProcessorContext(
        Park park,
        Mock<IParkPricingRepository> pricingRepository,
        bool apply)
    {
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        if (apply)
        {
            parkRepository
                .Setup(repository => repository.UpdateAsync("park-1", It.IsAny<Park>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string _, Park value, CancellationToken _) => value);
        }

        Mock<ISearchProjectionWriter> searchProjectionWriter = new(MockBehavior.Strict);
        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new(MockBehavior.Strict);
        if (apply)
        {
            searchProjectionWriter
                .Setup(writer => writer.UpsertAsync(SearchProjectionResourceTypes.Parks, "park-1", It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            publicSeoUpdateNotifier
                .Setup(notifier => notifier.NotifyAsync(It.IsAny<PublicSeoUpdate>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        Mock<IParkGraphUpsertHistoryRepository> historyRepository = new(MockBehavior.Strict);
        historyRepository
            .Setup(repository => repository.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new(
            parkRepository.Object,
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            historyRepository.Object,
            publicSeoUpdateNotifier.Object,
            MeasurementConversionService.Instance,
            pricingRepository.Object);

        return new ProcessorContext(
            processor,
            parkRepository,
            searchProjectionWriter,
            historyRepository,
            publicSeoUpdateNotifier);
    }

    private static ParkGraphUpsertRequest CreateRequest(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        return new ParkGraphUpsertRequest
        {
            TargetParkId = "park-1",
            CreateIfMissing = false,
            ReplaceCollections = false,
            Document = document.RootElement.Clone(),
            RawJson = json,
        };
    }

    private static string CreatePricingDocumentJson()
    {
        return """
        {
          "mode": "merge",
          "pricing": {
            "parkId": "park-1",
            "currencyCode": "EUR",
            "sourceUrl": "https://example.test/prices",
            "purchaseUrl": "https://example.test/tickets",
            "lastVerifiedAtUtc": "2026-08-09T10:00:00Z",
            "admissionOffers": [
              {
                "id": "admission-1",
                "code": "adult-high-season",
                "audienceCategory": "adult",
                "labels": [
                  { "languageCode": "fr", "value": "Adulte" },
                  { "languageCode": "en", "value": "Adult" },
                  { "languageCode": "es", "value": "Adulto" },
                  { "languageCode": "de", "value": "Erwachsene" },
                  { "languageCode": "it", "value": "Adulto" },
                  { "languageCode": "nl", "value": "Volwassene" },
                  { "languageCode": "pt", "value": "Adulto" },
                  { "languageCode": "pl", "value": "Dorosły" }
                ],
                "onlinePrice": { "mode": "Fixed", "amount": 39 },
                "gatePrice": { "mode": "Fixed", "amount": 45 },
                "validFrom": "2026-07-01",
                "validTo": "2026-08-31",
                "conditions": [],
                "sortOrder": 1
              }
            ],
            "annualPasses": [],
            "parkingOffers": []
          }
        }
        """;
    }

    private static Park CreateOperatingPark()
    {
        return new Park
        {
            Id = "park-1",
            Name = "Pricing Park",
            CountryCode = "FR",
            Status = ParkStatus.Operating,
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
    }

    private static ParkPricingEntity CreatePricing()
    {
        return new ParkPricingEntity
        {
            Id = "pricing-1",
            ParkId = "park-1",
            CurrencyCode = "EUR",
            SourceUrl = "https://example.test/prices",
            PurchaseUrl = "https://example.test/tickets",
            Notes = CreateLocalizedTexts("Prices may vary."),
            LastVerifiedAtUtc = new DateTime(2026, 8, 9, 10, 0, 0, DateTimeKind.Utc),
            AdmissionOffers = new List<ParkAdmissionPriceOffer>
            {
                new ParkAdmissionPriceOffer
                {
                    Id = "admission-1",
                    Code = "adult-high-season",
                    AudienceCategory = "adult",
                    Labels = CreateLocalizedTexts("Adult", "Adulte"),
                    OnlinePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 39m },
                    GatePrice = new ParkPriceValue { Mode = ParkPricingMode.Range, MinimumAmount = 45m, MaximumAmount = 55m },
                    ValidFrom = new DateOnly(2026, 7, 1),
                    ValidTo = new DateOnly(2026, 8, 31),
                    PurchaseUrl = "https://example.test/tickets/adult",
                    Conditions = CreateLocalizedTexts("Dated ticket.", "Billet daté."),
                    SortOrder = 1,
                },
            },
            AnnualPasses = new List<ParkAnnualPassOffer>
            {
                new ParkAnnualPassOffer
                {
                    Id = "pass-1",
                    Code = "gold",
                    Names = CreateLocalizedTexts("Gold pass", "Pass Gold"),
                    OnlinePrice = new ParkPriceValue
                    {
                        Mode = ParkPricingMode.Dynamic,
                        MinimumAmount = 199m,
                        MaximumAmount = 249m,
                    },
                    Conditions = new List<LocalizedText>(),
                    SortOrder = 2,
                },
            },
            ParkingOffers = new List<ParkParkingPriceOffer>
            {
                new ParkParkingPriceOffer
                {
                    Id = "parking-1",
                    Code = "car",
                    Labels = CreateLocalizedTexts("Car", "Voiture"),
                    GatePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 15m },
                    Conditions = new List<LocalizedText>(),
                    SortOrder = 3,
                },
            },
            HistoricalSnapshots = new List<ParkPricingSnapshot>
            {
                new ParkPricingSnapshot
                {
                    Id = "snapshot-2024",
                    Year = 2024,
                    CurrencyCode = "HRK",
                    SourceUrl = "https://example.test/prices/2024",
                    LastVerifiedAtUtc = new DateTime(2025, 1, 2, 10, 0, 0, DateTimeKind.Utc),
                    AdmissionOffers = new List<ParkAdmissionPriceOffer>
                    {
                        new ParkAdmissionPriceOffer
                        {
                            Id = "admission-2024-1",
                            Code = "adult-high-season",
                            AudienceCategory = "adult",
                            Labels = CreateLocalizedTexts("Adult", "Adulte"),
                            OnlinePrice = new ParkPriceValue { Mode = ParkPricingMode.Fixed, Amount = 300m },
                            Conditions = new List<LocalizedText>(),
                            SortOrder = 1,
                        },
                    },
                },
            },
        };
    }

    private static List<LocalizedText> CreateLocalizedTexts(string englishValue, string? frenchValue = null)
    {
        return new[] { "fr", "en", "es", "de", "it", "nl", "pt", "pl" }
            .Select(languageCode => new LocalizedText(
                languageCode,
                string.Equals(languageCode, "fr", StringComparison.Ordinal) ? frenchValue ?? englishValue : englishValue))
            .ToList();
    }

    private sealed class ProcessorContext
    {
        public ProcessorContext(
            ParkGraphUpsertProcessor processor,
            Mock<IParkRepository> parkRepository,
            Mock<ISearchProjectionWriter> searchProjectionWriter,
            Mock<IParkGraphUpsertHistoryRepository> historyRepository,
            Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier)
        {
            this.Processor = processor;
            this.ParkRepository = parkRepository;
            this.SearchProjectionWriter = searchProjectionWriter;
            this.HistoryRepository = historyRepository;
            this.PublicSeoUpdateNotifier = publicSeoUpdateNotifier;
        }

        public ParkGraphUpsertProcessor Processor { get; }

        private Mock<IParkRepository> ParkRepository { get; }

        private Mock<ISearchProjectionWriter> SearchProjectionWriter { get; }

        private Mock<IParkGraphUpsertHistoryRepository> HistoryRepository { get; }

        private Mock<IPublicSeoUpdateNotifier> PublicSeoUpdateNotifier { get; }

        public void VerifyAll()
        {
            this.ParkRepository.VerifyAll();
            this.SearchProjectionWriter.VerifyAll();
            this.HistoryRepository.VerifyAll();
            this.PublicSeoUpdateNotifier.VerifyAll();
        }
    }
}
