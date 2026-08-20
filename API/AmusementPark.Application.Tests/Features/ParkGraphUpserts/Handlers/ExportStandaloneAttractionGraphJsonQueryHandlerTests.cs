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
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
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
            .Setup(repository => repository.GetOwnerTimelineSummaryAsync(
                HistoryEntityType.StandaloneAttraction,
                "standalone-1",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { historyEvent });

        ExportParkGraphJsonQueryHandler handler = new ExportParkGraphJsonQueryHandler(
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            imageRepository.Object,
            historyEventRepository: historyRepository.Object,
            standaloneAttractionRepository: standaloneRepository.Object);

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

        standaloneRepository.VerifyAll();
        imageRepository.VerifyAll();
        historyRepository.VerifyAll();
    }
}
