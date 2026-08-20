using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Commands;
using AmusementPark.Application.Features.History.Contracts;
using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Handlers;

public sealed class StandaloneAttractionHistoryHandlersTests
{
    [Fact]
    public async Task UpsertHistoryEvent_WhenStandaloneAttractionExists_ShouldCreateEvent()
    {
        Mock<IHistoryEventRepository> historyRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IStandaloneAttractionRepository> standaloneAttractionRepository = new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapRefreshScheduler> sitemapRefreshScheduler = new Mock<ISeoSitemapRefreshScheduler>(MockBehavior.Strict);
        StandaloneAttraction attraction = new StandaloneAttraction
        {
            Id = "standalone-1",
            Name = "Pendolino",
            Type = ParkItemType.RollerCoaster,
            IsVisible = false,
            AdminReviewStatus = AdminReviewStatus.ToReview,
        };

        standaloneAttractionRepository
            .Setup(repository => repository.GetByIdAsync("standalone-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attraction);
        historyRepository
            .Setup(repository => repository.GetByOwnerKeyAsync(
                HistoryEntityType.StandaloneAttraction,
                "standalone-1",
                "pendolino-opening",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((HistoryEvent?)null);
        historyRepository
            .Setup(repository => repository.CreateAsync(
                It.Is<HistoryEvent>(historyEvent =>
                    historyEvent.EntityType == HistoryEntityType.StandaloneAttraction &&
                    historyEvent.OwnerId == "standalone-1" &&
                    historyEvent.ParkId == null &&
                    historyEvent.ParkItemId == null &&
                    historyEvent.EventType == ParkItemHistoryEventType.Opening.ToString()),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((HistoryEvent historyEvent, CancellationToken _) => historyEvent);
        sitemapRefreshScheduler
            .Setup(scheduler => scheduler.RequestRefreshAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        UpsertHistoryEventCommandHandler handler = new UpsertHistoryEventCommandHandler(
            historyRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            standaloneAttractionRepository.Object,
            sitemapRefreshScheduler.Object);

        ApplicationResult<HistoryEvent> result = await handler.HandleAsync(new UpsertHistoryEventCommand(new HistoryEventWriteModel
        {
            Key = "pendolino-opening",
            EntityType = HistoryEntityType.StandaloneAttraction,
            OwnerId = "standalone-1",
            Year = 2007,
            EventType = ParkItemHistoryEventType.Opening.ToString(),
            Titles = new[] { new LocalizedText("fr", "Ouverture de Pendolino") },
        }));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        historyRepository.VerifyAll();
        standaloneAttractionRepository.VerifyAll();
        parkRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyNoOtherCalls();
        sitemapRefreshScheduler.VerifyAll();
    }

    [Fact]
    public async Task GetStandaloneAttractionHistoryTimeline_WhenOpeningYearExists_ShouldReturnAutomaticOpening()
    {
        Mock<IHistoryEventRepository> historyRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        Mock<IStandaloneAttractionRepository> standaloneAttractionRepository = new Mock<IStandaloneAttractionRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        StandaloneAttraction attraction = new StandaloneAttraction
        {
            Id = "standalone-1",
            Name = "Pendolino",
            Type = ParkItemType.RollerCoaster,
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.Validated,
            AttractionDetails = new AttractionDetails
            {
                OpeningDateText = "2007",
            },
        };

        standaloneAttractionRepository
            .Setup(repository => repository.GetByIdAsync("standalone-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attraction);
        historyRepository
            .Setup(repository => repository.GetOwnerTimelineSummaryAsync(
                HistoryEntityType.StandaloneAttraction,
                "standalone-1",
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<HistoryEvent>());

        GetStandaloneAttractionHistoryTimelineQueryHandler handler = new GetStandaloneAttractionHistoryTimelineQueryHandler(
            historyRepository.Object,
            standaloneAttractionRepository.Object,
            imageRepository.Object);

        ApplicationResult<StandaloneAttractionHistoryTimelineResult> result = await handler.HandleAsync(
            new GetStandaloneAttractionHistoryTimelineQuery("standalone-1", false));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        HistoryTimelineEventResult timelineEvent = Assert.Single(result.Value!.Events);
        Assert.Equal(HistoryEntityType.StandaloneAttraction, timelineEvent.Event.EntityType);
        Assert.Equal("standalone-1", timelineEvent.Event.OwnerId);
        Assert.Equal(2007, timelineEvent.Event.Year);
        Assert.Equal(HistoryDatePrecision.Year, timelineEvent.Event.DatePrecision);
        Assert.Equal(ParkItemHistoryEventType.Opening.ToString(), timelineEvent.Event.EventType);
        Assert.StartsWith("auto-standalone-standalone-1-opening-2007", timelineEvent.Event.Key, StringComparison.Ordinal);
        historyRepository.VerifyAll();
        standaloneAttractionRepository.VerifyAll();
        imageRepository.VerifyNoOtherCalls();
    }
}
