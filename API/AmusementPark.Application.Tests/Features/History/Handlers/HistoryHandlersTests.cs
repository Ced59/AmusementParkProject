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
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Handlers;

public sealed class HistoryHandlersTests
{
    [Fact]
    public async Task UpsertHistoryEvent_WhenParkUsesParkItemOnlyType_ShouldFail()
    {
        Mock<IHistoryEventRepository> historyRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        UpsertHistoryEventCommandHandler handler = new UpsertHistoryEventCommandHandler(
            historyRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object);

        ApplicationResult<HistoryEvent> result = await handler.HandleAsync(new UpsertHistoryEventCommand(new HistoryEventWriteModel
        {
            EntityType = HistoryEntityType.Park,
            OwnerId = "park-1",
            Year = 1990,
            EventType = ParkItemHistoryEventType.Retrack.ToString(),
            Titles = new[] { new LocalizedText("fr", "Retrack impossible") },
        }));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "history.event-type.invalid");
        historyRepository.VerifyNoOtherCalls();
        parkRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpsertHistoryEvent_WhenParkItemEventIsValid_ShouldCreateEvent()
    {
        Mock<IHistoryEventRepository> historyRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        ParkItem item = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Mira Looping",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster,
            IsVisible = true,
        };

        parkItemRepository
            .Setup(repository => repository.GetByIdAsync("item-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        historyRepository
            .Setup(repository => repository.GetByOwnerKeyAsync(HistoryEntityType.ParkItem, "item-1", "mira-looping-retrack", It.IsAny<CancellationToken>()))
            .ReturnsAsync((HistoryEvent?)null);
        historyRepository
            .Setup(repository => repository.CreateAsync(It.Is<HistoryEvent>(historyEvent =>
                historyEvent.EntityType == HistoryEntityType.ParkItem &&
                historyEvent.OwnerId == "item-1" &&
                historyEvent.ParkItemId == "item-1" &&
                historyEvent.ContextParkId == "park-1" &&
                historyEvent.EventType == ParkItemHistoryEventType.Retrack.ToString()), It.IsAny<CancellationToken>()))
            .ReturnsAsync((HistoryEvent historyEvent, CancellationToken _) => historyEvent);

        UpsertHistoryEventCommandHandler handler = new UpsertHistoryEventCommandHandler(
            historyRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object);

        ApplicationResult<HistoryEvent> result = await handler.HandleAsync(new UpsertHistoryEventCommand(new HistoryEventWriteModel
        {
            Key = "mira-looping-retrack",
            EntityType = HistoryEntityType.ParkItem,
            OwnerId = "item-1",
            ParkId = "park-1",
            ContextParkId = "park-1",
            Year = 2001,
            EventType = ParkItemHistoryEventType.Retrack.ToString(),
            Titles = new[] { new LocalizedText("fr", "Retrack") },
        }));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        historyRepository.VerifyAll();
        parkRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyAll();
    }

    [Fact]
    public async Task GetParkHistoryTimeline_WhenNoEventExists_ShouldReturnNotFound()
    {
        Mock<IHistoryEventRepository> historyRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IImageRepository> imageRepository = new Mock<IImageRepository>(MockBehavior.Strict);
        Park park = new Park
        {
            Id = "park-1",
            Name = "Mirapolis",
            IsVisible = true,
        };

        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        historyRepository
            .Setup(repository => repository.GetParkTimelineAsync("park-1", false, false, It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 0), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<HistoryEvent>());

        GetParkHistoryTimelineQueryHandler handler = new GetParkHistoryTimelineQueryHandler(
            historyRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            imageRepository.Object);

        ApplicationResult<HistoryTimelineResult> result = await handler.HandleAsync(new GetParkHistoryTimelineQuery(
            "park-1",
            false,
            false,
            Array.Empty<string>()));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "history.not-found");
        historyRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
        imageRepository.VerifyNoOtherCalls();
    }
}
