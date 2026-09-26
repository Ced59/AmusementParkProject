using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Handlers;

public sealed class PublicParkHistoricalHandlersTests
{
    [Fact]
    public async Task Snapshot_WhenDayHasNoMonth_ReturnsValidationWithoutReadingData()
    {
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        PublicParkHistoricalDataLoader loader = CreateLoader(
            parkRepository,
            new Mock<IParkItemRepository>(MockBehavior.Strict),
            new Mock<IParkZoneRepository>(MockBehavior.Strict),
            new Mock<IHistoricalFactRepository>(MockBehavior.Strict));
        GetPublicParkHistoricalSnapshotQueryHandler handler = new(
            loader,
            new ParkHistoricalSnapshotBuilder());

        ApplicationResult<PublicParkHistoricalSnapshotResult> result = await handler.HandleAsync(
            new GetPublicParkHistoricalSnapshotQuery("park-1", 2001, null, 3));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "history.snapshot.date.invalid");
        parkRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Snapshot_IncludesCurrentPublicAndHistoricalOnlySubjects()
    {
        Park park = PublicParkHistoryTestData.CreatePark();
        ParkItem visibleItem = PublicParkHistoryTestData.CreateParkItem("visible", "Visible");
        ParkItem historicalItem = PublicParkHistoryTestData.CreateParkItem(
            "historical",
            "Historique",
            false);
        ParkItem hiddenFollowCurrentItem = PublicParkHistoryTestData.CreateParkItem(
            "hidden-follow",
            "Masqué",
            false);
        HistoricalFact historicalFact = PublicParkHistoryTestData.CreateOpeningFact(
            new HistoricalSubject(
                HistoricalSubjectType.ParkItem,
                historicalItem.Id,
                historicalItem.Name,
                HistoricalSubjectPublicationPolicy.HistoricalOnly),
            1995);
        HistoricalFact hiddenFollowCurrentFact = PublicParkHistoryTestData.CreateOpeningFact(
            new HistoricalSubject(
                HistoricalSubjectType.ParkItem,
                hiddenFollowCurrentItem.Id,
                hiddenFollowCurrentItem.Name,
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject),
            1996);
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new(MockBehavior.Strict);
        Mock<IParkZoneRepository> parkZoneRepository = new(MockBehavior.Strict);
        Mock<IHistoricalFactRepository> factRepository = new(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync(
                "park-1",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { visibleItem, historicalItem, hiddenFollowCurrentItem });
        parkZoneRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkZone>());
        factRepository
            .Setup(repository => repository.GetLatestRevisionsForSubjectsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { historicalFact, hiddenFollowCurrentFact });
        PublicParkHistoricalDataLoader loader = CreateLoader(
            parkRepository,
            parkItemRepository,
            parkZoneRepository,
            factRepository);
        GetPublicParkHistoricalSnapshotQueryHandler handler = new(
            loader,
            new ParkHistoricalSnapshotBuilder());

        ApplicationResult<PublicParkHistoricalSnapshotResult> result = await handler.HandleAsync(
            new GetPublicParkHistoricalSnapshotQuery("park-1", 1997, null, null));

        Assert.True(result.IsSuccess);
        PublicParkHistoricalSnapshotResult snapshot = Assert.IsType<PublicParkHistoricalSnapshotResult>(result.Value);
        Assert.Equal(3, snapshot.Snapshot.Subjects.Count);
        Assert.Contains(snapshot.Snapshot.Subjects, subject => subject.Subject.Id == park.Id);
        Assert.Contains(snapshot.Snapshot.Subjects, subject => subject.Subject.Id == visibleItem.Id);
        Assert.Contains(snapshot.Snapshot.Subjects, subject => subject.Subject.Id == historicalItem.Id);
        Assert.DoesNotContain(snapshot.Snapshot.Subjects, subject => subject.Subject.Id == hiddenFollowCurrentItem.Id);
        Assert.Single(snapshot.Facts);
        Assert.Equal(historicalFact.Id, snapshot.Facts.Single().Id);
    }

    [Fact]
    public async Task Timeline_PaginatesAndLoadsOnlySourcesForCurrentPage()
    {
        Park park = PublicParkHistoryTestData.CreatePark();
        HistoricalSubject parkSubject = new(
            HistoricalSubjectType.Park,
            park.Id,
            park.Name!,
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject);
        HistoricalFact first = PublicParkHistoryTestData.CreateOpeningFact(parkSubject, 1990);
        HistoricalFact second = PublicParkHistoryTestData.CreateOpeningFact(parkSubject, 2000);
        HistoricalFact third = PublicParkHistoryTestData.CreateOpeningFact(parkSubject, 2010);
        HistoricalSourceReference thirdSource = PublicParkHistoryTestData.CreateSource(third);
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new(MockBehavior.Strict);
        Mock<IParkZoneRepository> parkZoneRepository = new(MockBehavior.Strict);
        Mock<IHistoricalFactRepository> factRepository = new(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository = new(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync(
                "park-1",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());
        parkZoneRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkZone>());
        factRepository
            .Setup(repository => repository.GetLatestRevisionsForSubjectsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { second, third, first });
        sourceRepository
            .Setup(repository => repository.GetRevisionsAsync(
                It.Is<IReadOnlyCollection<HistoricalSourceRevisionReference>>(
                    references => references.Count == 1
                        && references.Single().SourceId == thirdSource.Id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { thirdSource });
        PublicParkHistoricalDataLoader loader = CreateLoader(
            parkRepository,
            parkItemRepository,
            parkZoneRepository,
            factRepository);
        GetPublicParkHistoricalTimelineQueryHandler handler = new(loader, sourceRepository.Object);

        ApplicationResult<PublicParkHistoricalTimelineResult> result = await handler.HandleAsync(
            new GetPublicParkHistoricalTimelineQuery("park-1", 2, 2));

        Assert.True(result.IsSuccess);
        PublicParkHistoricalTimelineResult timeline = Assert.IsType<PublicParkHistoricalTimelineResult>(
            result.Value);
        Assert.Equal(3, timeline.Page.TotalItems);
        PublicHistoricalTimelineEntryResult entry = Assert.Single(timeline.Page.Items);
        Assert.Equal(third.Id, entry.Fact.Id);
        Assert.Equal(thirdSource.Id, Assert.Single(entry.Sources).Id);
        sourceRepository.VerifyAll();
    }

    [Fact]
    public async Task Timeline_WhenPageSizeExceedsLimit_ReturnsValidationWithoutReadingData()
    {
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        PublicParkHistoricalDataLoader loader = CreateLoader(
            parkRepository,
            new Mock<IParkItemRepository>(MockBehavior.Strict),
            new Mock<IParkZoneRepository>(MockBehavior.Strict),
            new Mock<IHistoricalFactRepository>(MockBehavior.Strict));
        GetPublicParkHistoricalTimelineQueryHandler handler = new(
            loader,
            new Mock<IHistoricalSourceRepository>(MockBehavior.Strict).Object);

        ApplicationResult<PublicParkHistoricalTimelineResult> result = await handler.HandleAsync(
            new GetPublicParkHistoricalTimelineQuery("park-1", 1, 51));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "validation.pagination.invalid");
        parkRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Timeline_WhenParkIsHidden_ReturnsNotFoundWithoutLoadingChildren()
    {
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new(MockBehavior.Strict);
        Mock<IParkZoneRepository> parkZoneRepository = new(MockBehavior.Strict);
        Mock<IHistoricalFactRepository> factRepository = new(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(PublicParkHistoryTestData.CreatePark(false));
        PublicParkHistoricalDataLoader loader = CreateLoader(
            parkRepository,
            parkItemRepository,
            parkZoneRepository,
            factRepository);
        GetPublicParkHistoricalTimelineQueryHandler handler = new(
            loader,
            new Mock<IHistoricalSourceRepository>(MockBehavior.Strict).Object);

        ApplicationResult<PublicParkHistoricalTimelineResult> result = await handler.HandleAsync(
            new GetPublicParkHistoricalTimelineQuery("park-1"));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "park.not-found");
        parkItemRepository.VerifyNoOtherCalls();
        parkZoneRepository.VerifyNoOtherCalls();
        factRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Timeline_WhenPageIsArbitrarilyFar_ReturnsEmptyWithoutOverflowOrSourceRead()
    {
        Park park = PublicParkHistoryTestData.CreatePark();
        HistoricalSubject parkSubject = new(
            HistoricalSubjectType.Park,
            park.Id,
            park.Name!,
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject);
        HistoricalFact fact = PublicParkHistoryTestData.CreateOpeningFact(parkSubject, 2000);
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new(MockBehavior.Strict);
        Mock<IParkZoneRepository> parkZoneRepository = new(MockBehavior.Strict);
        Mock<IHistoricalFactRepository> factRepository = new(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository = new(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync(
                "park-1",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());
        parkZoneRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkZone>());
        factRepository
            .Setup(repository => repository.GetLatestRevisionsForSubjectsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { fact });
        PublicParkHistoricalDataLoader loader = CreateLoader(
            parkRepository,
            parkItemRepository,
            parkZoneRepository,
            factRepository);
        GetPublicParkHistoricalTimelineQueryHandler handler = new(loader, sourceRepository.Object);

        ApplicationResult<PublicParkHistoricalTimelineResult> result = await handler.HandleAsync(
            new GetPublicParkHistoricalTimelineQuery("park-1", int.MaxValue, 50));

        Assert.True(result.IsSuccess);
        Assert.Empty(Assert.IsType<PublicParkHistoricalTimelineResult>(result.Value).Page.Items);
        sourceRepository.VerifyNoOtherCalls();
    }

    private static PublicParkHistoricalDataLoader CreateLoader(
        Mock<IParkRepository> parkRepository,
        Mock<IParkItemRepository> parkItemRepository,
        Mock<IParkZoneRepository> parkZoneRepository,
        Mock<IHistoricalFactRepository> factRepository)
    {
        return new PublicParkHistoricalDataLoader(
            parkRepository.Object,
            parkItemRepository.Object,
            parkZoneRepository.Object,
            factRepository.Object);
    }
}
