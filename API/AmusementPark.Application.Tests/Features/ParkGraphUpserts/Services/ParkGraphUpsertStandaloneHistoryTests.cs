using System.Text.Json;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Application.Features.ParkGraphUpserts.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.ParkGraphUpserts.Services;

public sealed class ParkGraphUpsertStandaloneHistoryTests
{
    [Fact]
    public async Task PreviewAndApplyAsync_WhenStandaloneGraphContainsHistory_ShouldCreateStandaloneEvent()
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
        Mock<IStandaloneAttractionRepository> standaloneRepository = new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict);
        standaloneRepository
            .Setup(repository => repository.GetByIdAsync("standalone-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attraction);

        HistoryEvent? persistedEvent = null;
        Mock<IHistoryEventRepository> historyEventRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        historyEventRepository
            .Setup(repository => repository.GetByOwnerKeyAsync(
                HistoryEntityType.StandaloneAttraction,
                "standalone-1",
                "pendolino-opening-2007",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => persistedEvent);
        historyEventRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<HistoryEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((HistoryEvent historyEvent, CancellationToken _) =>
            {
                historyEvent.Id = "history-1";
                persistedEvent = historyEvent;
                return historyEvent;
            });

        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        searchProjectionWriter
            .Setup(writer => writer.UpsertAsync(
                SearchProjectionResourceTypes.StandaloneAttractions,
                "standalone-1",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Mock<IParkGraphUpsertHistoryRepository> upsertHistoryRepository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        upsertHistoryRepository
            .Setup(repository => repository.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = new ParkGraphUpsertProcessor(
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IParkZoneRepository>(MockBehavior.Strict),
            Mock.Of<IParkItemRepository>(MockBehavior.Strict),
            Mock.Of<IParkFounderRepository>(MockBehavior.Strict),
            Mock.Of<IParkOperatorRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<IImageRepository>(MockBehavior.Strict),
            Mock.Of<IRemoteImageImporter>(MockBehavior.Strict),
            searchProjectionWriter.Object,
            upsertHistoryRepository.Object,
            Mock.Of<IPublicSeoUpdateNotifier>(MockBehavior.Strict),
            MeasurementConversionService.Instance,
            historyEventRepository: historyEventRepository.Object,
            standaloneAttractionRepository: standaloneRepository.Object);

        const string rawJson = """
        {
          "documentType": "standaloneAttractionGraph",
          "schemaVersion": "2026-07-16",
          "mode": "merge",
          "identity": {
            "standaloneAttractionId": "standalone-1"
          },
          "standaloneAttraction": {
            "id": "standalone-1",
            "name": "Pendolino",
            "countryCode": "AT",
            "type": "RollerCoaster",
            "isVisible": false,
            "adminReviewStatus": "ToReview"
          },
          "history": {
            "events": [
              {
                "key": "pendolino-opening-2007",
                "entityType": "StandaloneAttraction",
                "ownerId": "standalone-1",
                "date": "2007",
                "eventType": "Opening",
                "isMajor": true,
                "isVisible": true,
                "titles": {
                  "fr": "Ouverture de Pendolino",
                  "en": "Pendolino opens"
                }
              }
            ]
          }
        }
        """;

        using JsonDocument previewDocument = JsonDocument.Parse(rawJson);
        ParkGraphUpsertRequest previewRequest = new ParkGraphUpsertRequest
        {
            CreateIfMissing = false,
            ReplaceCollections = false,
            Document = previewDocument.RootElement.Clone(),
            RawJson = rawJson,
        };

        ApplicationResult<ParkGraphUpsertResult> preview = await processor.PreviewAsync(previewRequest, "user-1", CancellationToken.None);

        Assert.True(preview.IsSuccess);
        Assert.Empty(preview.Value!.Errors);
        ParkGraphUpsertChange previewHistoryChange = Assert.Single(
            preview.Value.Changes,
            static change => change.EntityType == "HistoryEvent");
        Assert.Equal("Created", previewHistoryChange.ChangeType);
        historyEventRepository.Verify(
            repository => repository.CreateAsync(It.IsAny<HistoryEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);

        using JsonDocument applyDocument = JsonDocument.Parse(rawJson);
        ParkGraphUpsertRequest applyRequest = new ParkGraphUpsertRequest
        {
            CreateIfMissing = false,
            ReplaceCollections = false,
            Document = applyDocument.RootElement.Clone(),
            RawJson = rawJson,
        };

        ApplicationResult<ParkGraphUpsertResult> apply = await processor.ApplyAsync(applyRequest, "user-1", CancellationToken.None);

        Assert.True(apply.IsSuccess);
        Assert.Empty(apply.Value!.Errors);
        HistoryEvent created = Assert.IsType<HistoryEvent>(persistedEvent);
        Assert.Equal(HistoryEntityType.StandaloneAttraction, created.EntityType);
        Assert.Equal("standalone-1", created.OwnerId);
        Assert.Null(created.ParkId);
        Assert.Null(created.ParkItemId);
        Assert.Equal(2007, created.Year);
        Assert.Equal(HistoryDatePrecision.Year, created.DatePrecision);
        Assert.Equal(ParkItemHistoryEventType.Opening.ToString(), created.EventType);
        Assert.True(created.IsVisible);
        historyEventRepository.Verify(
            repository => repository.CreateAsync(It.IsAny<HistoryEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
        searchProjectionWriter.VerifyAll();
        upsertHistoryRepository.Verify(
            repository => repository.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }
}
