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
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
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
        Mock<ISeoSitemapRefreshScheduler> sitemapRefreshScheduler = new Mock<ISeoSitemapRefreshScheduler>(MockBehavior.Strict);
        UpsertHistoryEventCommandHandler handler = new UpsertHistoryEventCommandHandler(
            historyRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            sitemapRefreshScheduler.Object);

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
        sitemapRefreshScheduler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpsertHistoryEvent_WhenParkItemEventIsValid_ShouldCreateEvent()
    {
        Mock<IHistoryEventRepository> historyRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapRefreshScheduler> sitemapRefreshScheduler = new Mock<ISeoSitemapRefreshScheduler>(MockBehavior.Strict);
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
        sitemapRefreshScheduler
            .Setup(scheduler => scheduler.RequestRefreshAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        UpsertHistoryEventCommandHandler handler = new UpsertHistoryEventCommandHandler(
            historyRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            sitemapRefreshScheduler.Object);

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
        sitemapRefreshScheduler.VerifyAll();
    }

    [Fact]
    public async Task GetParkHistoryTimeline_WhenParkHasLifecycleDates_ShouldReturnAutomaticEvents()
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
            OpeningDate = new DateTime(1987, 5, 20),
            OpeningDateText = "1987-05-20",
            ClosingDate = new DateTime(1991, 1, 1),
            ClosingDateText = "1991",
        };

        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { park });
        historyRepository
            .Setup(repository => repository.GetParkTimelineAsync("park-1", false, false, It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 0), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<HistoryEvent>());
        historyRepository
            .Setup(repository => repository.HasParkItemTimelineEventsAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());

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

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.HasParkItemTimelineEvents);
        Assert.Equal(new[] { ParkHistoryEventType.Opening.ToString(), ParkHistoryEventType.DefinitiveClosure.ToString() }, result.Value.Events.Select(entry => entry.Event.EventType));
        Assert.Equal(HistoryDatePrecision.Day, result.Value.Events.First().Event.DatePrecision);
        Assert.Equal(HistoryDatePrecision.Year, result.Value.Events.Last().Event.DatePrecision);
        Assert.All(result.Value.Events, entry => Assert.StartsWith("auto-park-park-1-", entry.Event.Id, StringComparison.Ordinal));
        historyRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetParkHistoryTimeline_WhenManualParkLifecycleEventExists_ShouldSuppressAutomaticDuplicate()
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
            OpeningDate = new DateTime(1987, 5, 20),
            OpeningDateText = "1987-05-20",
            ClosingDate = new DateTime(1991, 10, 20),
            ClosingDateText = "1991-10-20",
        };
        HistoryEvent manualOpening = new HistoryEvent
        {
            Id = "manual-opening",
            Key = "manual-opening",
            EntityType = HistoryEntityType.Park,
            OwnerId = "park-1",
            ParkId = "park-1",
            Year = 1987,
            Month = 5,
            Day = 20,
            DatePrecision = HistoryDatePrecision.Day,
            EventType = ParkHistoryEventType.Opening.ToString(),
            IsVisible = true,
        };

        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { park });
        historyRepository
            .Setup(repository => repository.GetParkTimelineAsync("park-1", false, false, It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 0), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { manualOpening });
        historyRepository
            .Setup(repository => repository.HasParkItemTimelineEventsAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());

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

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Events.Count);
        Assert.Single(result.Value.Events, entry => entry.Event.EventType == ParkHistoryEventType.Opening.ToString());
        Assert.Contains(result.Value.Events, static entry => entry.Event.Id == "manual-opening");
        Assert.Contains(result.Value.Events, static entry => entry.Event.EventType == ParkHistoryEventType.DefinitiveClosure.ToString());
        historyRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyNoOtherCalls();
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

    [Fact]
    public async Task GetParkHistoryTimeline_WhenIncludingParkItemsWithLifecycleDates_ShouldReturnAutomaticEvents()
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
        ParkItem item = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Mira Looping",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster,
            IsVisible = true,
            AttractionDetails = new AttractionDetails
            {
                OpeningDate = new DateTime(1988, 1, 1),
                OpeningDateText = "1988",
                ClosingDate = new DateTime(1991, 10, 20),
                ClosingDateText = "1991-10-20",
            },
        };

        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { park });
        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });
        parkItemRepository
            .Setup(repository => repository.GetByIdsAsync(It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "item-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });
        historyRepository
            .Setup(repository => repository.GetParkTimelineAsync("park-1", false, true, It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 0), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<HistoryEvent>());

        GetParkHistoryTimelineQueryHandler handler = new GetParkHistoryTimelineQueryHandler(
            historyRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            imageRepository.Object);

        ApplicationResult<HistoryTimelineResult> result = await handler.HandleAsync(new GetParkHistoryTimelineQuery(
            "park-1",
            false,
            true,
            Array.Empty<string>()));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.HasParkItemTimelineEvents);
        Assert.Equal(new[] { ParkItemHistoryEventType.Opening.ToString(), ParkItemHistoryEventType.DefinitiveClosure.ToString() }, result.Value.Events.Select(entry => entry.Event.EventType));
        Assert.Equal(HistoryDatePrecision.Year, result.Value.Events.First().Event.DatePrecision);
        Assert.Equal(HistoryDatePrecision.Day, result.Value.Events.Last().Event.DatePrecision);
        Assert.Single(result.Value.IncludedParkItems);
        historyRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetParkItemHistoryTimeline_WhenItemHasLifecycleDates_ShouldReturnAutomaticEvents()
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
        ParkItem item = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Mira Looping",
            Category = ParkItemCategory.Attraction,
            Type = ParkItemType.RollerCoaster,
            IsVisible = true,
            AttractionDetails = new AttractionDetails
            {
                OpeningDate = new DateTime(1988, 1, 1),
                OpeningDateText = "1988",
                ClosingDate = new DateTime(1991, 10, 20),
            },
        };

        parkItemRepository
            .Setup(repository => repository.GetByIdAsync("item-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        parkItemRepository
            .Setup(repository => repository.GetByIdsAsync(It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "item-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { park });
        historyRepository
            .Setup(repository => repository.GetOwnerTimelineAsync(HistoryEntityType.ParkItem, "item-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<HistoryEvent>());

        GetParkItemHistoryTimelineQueryHandler handler = new GetParkItemHistoryTimelineQueryHandler(
            historyRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            imageRepository.Object);

        ApplicationResult<HistoryTimelineResult> result = await handler.HandleAsync(new GetParkItemHistoryTimelineQuery("item-1", false));

        Assert.True(result.IsSuccess);
        Assert.Equal(HistoryEntityType.ParkItem, result.Value!.EntityType);
        Assert.Equal("item-1", result.Value.ParkItem!.Id);
        Assert.Equal(new[] { ParkItemHistoryEventType.Opening.ToString(), ParkItemHistoryEventType.DefinitiveClosure.ToString() }, result.Value.Events.Select(entry => entry.Event.EventType));
        Assert.All(result.Value.Events, entry => Assert.StartsWith("auto-parkitem-item-1-park-1-", entry.Event.Id, StringComparison.Ordinal));
        historyRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetParkHistoryTimeline_WhenParkItemEventsExistButAreNotIncluded_ShouldExposeAvailability()
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
        HistoryEvent openingEvent = new HistoryEvent
        {
            Id = "history-1",
            Key = "mirapolis-opening",
            EntityType = HistoryEntityType.Park,
            OwnerId = "park-1",
            ParkId = "park-1",
            Year = 1987,
            Month = 5,
            Day = 20,
            DatePrecision = HistoryDatePrecision.Day,
            EventType = ParkHistoryEventType.Opening.ToString(),
            IsVisible = true,
        };

        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { park });
        historyRepository
            .Setup(repository => repository.GetParkTimelineAsync("park-1", false, false, It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 0), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { openingEvent });
        historyRepository
            .Setup(repository => repository.HasParkItemTimelineEventsAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

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

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.HasParkItemTimelineEvents);
        Assert.Empty(result.Value.IncludedParkItems);
        historyRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
        imageRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetParkHistoryTimeline_WhenTimelineIsPaged_ShouldReturnRequestedPageAndRanges()
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
        HistoryEvent firstEvent = CreateParkHistoryEvent("event-1980", 1980);
        HistoryEvent secondEvent = CreateParkHistoryEvent("event-1985", 1985);
        HistoryEvent thirdEvent = CreateParkHistoryEvent("event-1990", 1990);

        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { park });
        historyRepository
            .Setup(repository => repository.GetParkTimelineAsync("park-1", false, false, It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 0), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { firstEvent, secondEvent, thirdEvent });
        historyRepository
            .Setup(repository => repository.HasParkItemTimelineEventsAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());

        GetParkHistoryTimelineQueryHandler handler = new GetParkHistoryTimelineQueryHandler(
            historyRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            imageRepository.Object);

        ApplicationResult<HistoryTimelineResult> result = await handler.HandleAsync(new GetParkHistoryTimelineQuery(
            "park-1",
            false,
            false,
            Array.Empty<string>(),
            Page: 2,
            PageSize: 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "event-1990" }, result.Value!.Events.Select(entry => entry.Event.Id));
        Assert.Equal(3, result.Value.Pagination.TotalItems);
        Assert.Equal(2, result.Value.Pagination.TotalPages);
        Assert.Equal(2, result.Value.Pagination.CurrentPage);
        Assert.Equal(2, result.Value.Pagination.ItemsPerPage);
        Assert.Equal(new[] { 1, 2 }, result.Value.PageRanges.Select(range => range.Page));
        Assert.Equal(new[] { 1980, 1990 }, result.Value.PageRanges.Select(range => range.StartYear));
        Assert.Equal(new[] { 1985, 1990 }, result.Value.PageRanges.Select(range => range.EndYear));
        historyRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetParkHistoryTimeline_WhenTimelineIsPaged_ShouldHydrateImagesForCurrentPageOnly()
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
        HistoryEvent firstEvent = CreateParkHistoryEvent("event-1980", 1980);
        firstEvent.MainImageId = "image-1980";
        HistoryEvent secondEvent = CreateParkHistoryEvent("event-1985", 1985);
        secondEvent.MainImageId = "image-1985";
        HistoryEvent thirdEvent = CreateParkHistoryEvent("event-1990", 1990);
        thirdEvent.MainImageId = "image-1990";
        Image pageImage = new Image
        {
            Id = "image-1985",
            Path = "history/image-1985.jpg",
            IsPublished = true,
        };

        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { park });
        historyRepository
            .Setup(repository => repository.GetParkTimelineAsync("park-1", false, false, It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 0), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { firstEvent, secondEvent, thirdEvent });
        historyRepository
            .Setup(repository => repository.HasParkItemTimelineEventsAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());
        imageRepository
            .Setup(repository => repository.GetByIdAsync("image-1985", It.IsAny<CancellationToken>()))
            .ReturnsAsync(pageImage);

        GetParkHistoryTimelineQueryHandler handler = new GetParkHistoryTimelineQueryHandler(
            historyRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            imageRepository.Object);

        ApplicationResult<HistoryTimelineResult> result = await handler.HandleAsync(new GetParkHistoryTimelineQuery(
            "park-1",
            false,
            false,
            Array.Empty<string>(),
            Page: 2,
            PageSize: 1));

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "event-1985" }, result.Value!.Events.Select(entry => entry.Event.Id));
        Assert.Same(pageImage, result.Value.Events.Single().MainImage);
        historyRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyAll();
        imageRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetParkHistoryTimeline_WhenRequestedPageExceedsTotalPages_ShouldReturnLastPage()
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
        HistoryEvent firstEvent = CreateParkHistoryEvent("event-1980", 1980);
        HistoryEvent secondEvent = CreateParkHistoryEvent("event-1985", 1985);
        HistoryEvent thirdEvent = CreateParkHistoryEvent("event-1990", 1990);

        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "park-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { park });
        historyRepository
            .Setup(repository => repository.GetParkTimelineAsync("park-1", false, false, It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 0), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { firstEvent, secondEvent, thirdEvent });
        historyRepository
            .Setup(repository => repository.HasParkItemTimelineEventsAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());

        GetParkHistoryTimelineQueryHandler handler = new GetParkHistoryTimelineQueryHandler(
            historyRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            imageRepository.Object);

        ApplicationResult<HistoryTimelineResult> result = await handler.HandleAsync(new GetParkHistoryTimelineQuery(
            "park-1",
            false,
            false,
            Array.Empty<string>(),
            Page: 99,
            PageSize: 2));

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "event-1990" }, result.Value!.Events.Select(entry => entry.Event.Id));
        Assert.Equal(2, result.Value.Pagination.TotalPages);
        Assert.Equal(2, result.Value.Pagination.CurrentPage);
        historyRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetParkHistoryTimeline_WhenIncludedParkItemOwnerIsNotPublic_ShouldReturnNotFound()
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
        ParkItem parkItem = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Hidden ride",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.NotRelevant,
        };
        HistoryEvent parkItemEvent = new HistoryEvent
        {
            Id = "history-1",
            Key = "hidden-ride-opening",
            EntityType = HistoryEntityType.ParkItem,
            OwnerId = "item-1",
            ParkId = "park-1",
            ParkItemId = "item-1",
            ContextParkId = "park-1",
            Year = 1988,
            EventType = ParkItemHistoryEventType.Opening.ToString(),
            IsVisible = true,
        };

        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { park });
        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());
        parkItemRepository
            .Setup(repository => repository.GetByIdsAsync(It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "item-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { parkItem });
        historyRepository
            .Setup(repository => repository.GetParkTimelineAsync("park-1", false, true, It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 0), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { parkItemEvent });

        GetParkHistoryTimelineQueryHandler handler = new GetParkHistoryTimelineQueryHandler(
            historyRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            imageRepository.Object);

        ApplicationResult<HistoryTimelineResult> result = await handler.HandleAsync(new GetParkHistoryTimelineQuery(
            "park-1",
            false,
            true,
            Array.Empty<string>()));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "history.not-found");
        historyRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetParkItemHistoryTimeline_WhenParentParkIsNotPublic_ShouldReturnNotFound()
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
            AdminReviewStatus = AdminReviewStatus.NotRelevant,
        };
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
            .Setup(repository => repository.GetByIdAsync("item-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);

        GetParkItemHistoryTimelineQueryHandler handler = new GetParkItemHistoryTimelineQueryHandler(
            historyRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            imageRepository.Object);

        ApplicationResult<HistoryTimelineResult> result = await handler.HandleAsync(new GetParkItemHistoryTimelineQuery("item-1", false));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "parkitem.not-found");
        historyRepository.VerifyNoOtherCalls();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetHistoryArticle_WhenParkItemOwnerIsNotPublic_ShouldReturnNotFound()
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
        ParkItem parkItem = new ParkItem
        {
            Id = "item-1",
            ParkId = "park-1",
            Name = "Hidden ride",
            IsVisible = true,
            AdminReviewStatus = AdminReviewStatus.NotRelevant,
        };
        HistoryEvent historyEvent = new HistoryEvent
        {
            Id = "history-1",
            Key = "hidden-ride-opening",
            EntityType = HistoryEntityType.ParkItem,
            OwnerId = "item-1",
            ParkId = "park-1",
            ParkItemId = "item-1",
            ContextParkId = "park-1",
            Year = 1988,
            EventType = ParkItemHistoryEventType.Opening.ToString(),
            IsMajor = true,
            IsVisible = true,
            Article = new HistoryArticle
            {
                Slug = "hidden-ride-opening",
                IsPublished = true,
            },
        };

        historyRepository
            .Setup(repository => repository.GetByIdAsync("history-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(historyEvent);
        parkRepository
            .Setup(repository => repository.GetByIdsAsync(It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "park-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { park });
        parkItemRepository
            .Setup(repository => repository.GetByIdsAsync(It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "item-1" })), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { parkItem });

        GetHistoryArticleQueryHandler handler = new GetHistoryArticleQueryHandler(
            historyRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object,
            imageRepository.Object);

        ApplicationResult<HistoryArticleResult> result = await handler.HandleAsync(new GetHistoryArticleQuery("history-1", false));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "history.article.not-found");
        historyRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        imageRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteHistoryEvent_WhenEventExists_ShouldRefreshSitemap()
    {
        Mock<IHistoryEventRepository> historyRepository = new Mock<IHistoryEventRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapRefreshScheduler> sitemapRefreshScheduler = new Mock<ISeoSitemapRefreshScheduler>(MockBehavior.Strict);
        historyRepository
            .Setup(repository => repository.DeleteAsync("event-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        sitemapRefreshScheduler
            .Setup(scheduler => scheduler.RequestRefreshAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        DeleteHistoryEventCommandHandler handler = new DeleteHistoryEventCommandHandler(
            historyRepository.Object,
            sitemapRefreshScheduler.Object);

        ApplicationResult result = await handler.HandleAsync(new DeleteHistoryEventCommand("event-1"));

        Assert.True(result.IsSuccess);
        historyRepository.VerifyAll();
        sitemapRefreshScheduler.VerifyAll();
    }

    private static HistoryEvent CreateParkHistoryEvent(string id, int year)
    {
        return new HistoryEvent
        {
            Id = id,
            Key = id,
            EntityType = HistoryEntityType.Park,
            OwnerId = "park-1",
            ParkId = "park-1",
            Year = year,
            DatePrecision = HistoryDatePrecision.Year,
            EventType = ParkHistoryEventType.Redevelopment.ToString(),
            IsVisible = true,
        };
    }
}
