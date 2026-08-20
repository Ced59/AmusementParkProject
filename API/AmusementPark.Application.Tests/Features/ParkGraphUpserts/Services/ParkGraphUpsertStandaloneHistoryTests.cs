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
using AmusementPark.Application.Features.Seo.Models;
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
        StandaloneAttraction attraction = CreateAttraction();
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

        Mock<IParkGraphUpsertHistoryRepository> upsertHistoryRepository = CreateUpsertHistoryRepository();
        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);
        publicSeoUpdateNotifier
            .Setup(notifier => notifier.NotifyAsync(It.IsAny<PublicSeoUpdate>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        ParkGraphUpsertProcessor processor = CreateProcessor(
            standaloneRepository,
            historyEventRepository,
            searchProjectionWriter,
            upsertHistoryRepository,
            publicSeoUpdateNotifier);

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
        ParkGraphUpsertRequest previewRequest = CreateRequest(previewDocument, rawJson);

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
        ParkGraphUpsertRequest applyRequest = CreateRequest(applyDocument, rawJson);

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
        publicSeoUpdateNotifier.Verify(
            notifier => notifier.NotifyAsync(It.IsAny<PublicSeoUpdate>(), It.IsAny<CancellationToken>()),
            Times.Once);
        upsertHistoryRepository.Verify(
            repository => repository.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ApplyAsync_WhenLaterStandaloneHistoryEventIsInvalid_ShouldNotPersistEarlierChanges()
    {
        StandaloneAttraction attraction = CreateAttraction();
        Mock<IStandaloneAttractionRepository> standaloneRepository = new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict);
        standaloneRepository
            .Setup(repository => repository.GetByIdAsync("standalone-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attraction);
        Mock<IHistoryEventRepository> historyEventRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        Mock<ISearchProjectionWriter> searchProjectionWriter = new Mock<ISearchProjectionWriter>(MockBehavior.Strict);
        Mock<IParkGraphUpsertHistoryRepository> upsertHistoryRepository = CreateUpsertHistoryRepository();
        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier = new Mock<IPublicSeoUpdateNotifier>(MockBehavior.Strict);

        ParkGraphUpsertProcessor processor = CreateProcessor(
            standaloneRepository,
            historyEventRepository,
            searchProjectionWriter,
            upsertHistoryRepository,
            publicSeoUpdateNotifier);

        const string rawJson = """
        {
          "documentType": "standaloneAttractionGraph",
          "identity": { "standaloneAttractionId": "standalone-1" },
          "standaloneAttraction": {
            "id": "standalone-1",
            "name": "Pendolino"
          },
          "history": {
            "events": [
              {
                "key": "pendolino-opening-2007",
                "entityType": "StandaloneAttraction",
                "ownerId": "standalone-1",
                "date": "2007",
                "eventType": "Opening"
              },
              {
                "key": "invalid-event",
                "entityType": "StandaloneAttraction",
                "ownerId": "standalone-1",
                "date": "2008",
                "eventType": "DefinitelyNotAHistoryType"
              }
            ]
          }
        }
        """;

        using JsonDocument document = JsonDocument.Parse(rawJson);
        ApplicationResult<ParkGraphUpsertResult> result = await processor.ApplyAsync(
            CreateRequest(document, rawJson),
            "user-1",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        standaloneRepository.Verify(
            repository => repository.UpdateAsync(It.IsAny<string>(), It.IsAny<StandaloneAttraction>(), It.IsAny<CancellationToken>()),
            Times.Never);
        historyEventRepository.Verify(
            repository => repository.CreateAsync(It.IsAny<HistoryEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        searchProjectionWriter.VerifyNoOtherCalls();
        publicSeoUpdateNotifier.VerifyNoOtherCalls();
        upsertHistoryRepository.Verify(
            repository => repository.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static StandaloneAttraction CreateAttraction()
    {
        return new StandaloneAttraction
        {
            Id = "standalone-1",
            Name = "Pendolino",
            CountryCode = "AT",
            Type = ParkItemType.RollerCoaster,
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };
    }

    private static ParkGraphUpsertRequest CreateRequest(JsonDocument document, string rawJson)
    {
        return new ParkGraphUpsertRequest
        {
            CreateIfMissing = false,
            ReplaceCollections = false,
            Document = document.RootElement.Clone(),
            RawJson = rawJson,
        };
    }

    private static Mock<IParkGraphUpsertHistoryRepository> CreateUpsertHistoryRepository()
    {
        Mock<IParkGraphUpsertHistoryRepository> repository = new Mock<IParkGraphUpsertHistoryRepository>(MockBehavior.Strict);
        repository
            .Setup(item => item.SaveAsync(It.IsAny<ParkGraphUpsertHistoryEntry>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return repository;
    }

    private static ParkGraphUpsertProcessor CreateProcessor(
        Mock<IStandaloneAttractionRepository> standaloneRepository,
        Mock<IHistoryEventRepository> historyEventRepository,
        Mock<ISearchProjectionWriter> searchProjectionWriter,
        Mock<IParkGraphUpsertHistoryRepository> upsertHistoryRepository,
        Mock<IPublicSeoUpdateNotifier> publicSeoUpdateNotifier)
    {
        return new ParkGraphUpsertProcessor(
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
            publicSeoUpdateNotifier.Object,
            MeasurementConversionService.Instance,
            historyEventRepository: historyEventRepository.Object,
            standaloneAttractionRepository: standaloneRepository.Object);
    }
}
