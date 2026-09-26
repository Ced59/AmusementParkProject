using AmusementPark.Application.Errors;
using AmusementPark.Application.Common.Results;
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
                "Libellé historique figé",
                HistoricalSubjectPublicationPolicy.HistoricalOnly,
                park.Id),
            1995);
        HistoricalFact hiddenFollowCurrentFact = PublicParkHistoryTestData.CreateOpeningFact(
            new HistoricalSubject(
                HistoricalSubjectType.ParkItem,
                hiddenFollowCurrentItem.Id,
                hiddenFollowCurrentItem.Name,
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                park.Id),
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
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { visibleItem, historicalItem, hiddenFollowCurrentItem });
        parkZoneRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkZone>());
        factRepository
            .Setup(repository => repository.GetLatestDecisionEligibleRevisionsForParkAsync(
                "park-1",
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
        Assert.Contains(
            snapshot.Snapshot.Subjects,
            subject => subject.Subject.Id == historicalItem.Id
                && subject.Subject.HistoricalLabel == "Libellé historique figé");
        Assert.DoesNotContain(snapshot.Snapshot.Subjects, subject => subject.Subject.Id == hiddenFollowCurrentItem.Id);
        Assert.Single(snapshot.Facts);
        Assert.Equal(historicalFact.Id, snapshot.Facts.Single().Id);
    }

    [Fact]
    public async Task Timeline_LoadsCurrentPageAndExposesNarrativesOnlyForCurrentPublicSubjects()
    {
        Park park = PublicParkHistoryTestData.CreatePark();
        ParkItem visibleItem = PublicParkHistoryTestData.CreateParkItem("visible-item", "Attraction visible");
        HistoricalSubject visibleSubject = new(
            HistoricalSubjectType.ParkItem,
            visibleItem.Id,
            "Ancien nom documenté",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            park.Id);
        HistoricalSubject removedSubject = new(
            HistoricalSubjectType.ParkItem,
            "removed-item",
            "Attraction disparue",
            HistoricalSubjectPublicationPolicy.HistoricalOnly,
            park.Id);
        HistoricalFact visibleFact = PublicParkHistoryTestData.CreateOpeningFact(
            visibleSubject,
            2010,
            narrativeContentId: "event-visible");
        HistoricalFact removedFact = PublicParkHistoryTestData.CreateOpeningFact(
            removedSubject,
            1990,
            narrativeContentId: "event-removed");
        HistoricalSourceReference visibleSource = PublicParkHistoryTestData.CreateSource(visibleFact);
        HistoricalSourceReference removedSource = PublicParkHistoryTestData.CreateSource(removedFact);
        HistoryEvent visibleNarrative = new()
        {
            Id = "event-visible",
            EntityType = HistoryEntityType.ParkItem,
            OwnerId = visibleItem.Id,
            IsVisible = true,
            IsMajor = true,
            Article = new HistoryArticle { IsPublished = true },
        };
        HistoryEvent removedNarrative = new()
        {
            Id = "event-removed",
            EntityType = HistoryEntityType.ParkItem,
            OwnerId = removedSubject.Id,
            IsVisible = true,
            IsMajor = true,
            Article = new HistoryArticle { IsPublished = true },
        };
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new(MockBehavior.Strict);
        Mock<IParkZoneRepository> parkZoneRepository = new(MockBehavior.Strict);
        Mock<IHistoricalFactRepository> factRepository = new(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository = new(MockBehavior.Strict);
        Mock<IHistoryEventRepository> historyEventRepository = new(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync(
                "park-1",
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { visibleItem });
        parkZoneRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkZone>());
        factRepository
            .Setup(repository => repository.GetLatestDecisionEligibleRevisionsForParkPageAsync(
                "park-1",
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                2,
                2,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<HistoricalFact>(new[] { visibleFact, removedFact }, 2, 2, 4));
        sourceRepository
            .Setup(repository => repository.GetRevisionsAsync(
                It.Is<IReadOnlyCollection<HistoricalSourceRevisionReference>>(
                    references => references.Count == 2
                        && references.Any(reference => reference.SourceId == visibleSource.Id)
                        && references.Any(reference => reference.SourceId == removedSource.Id)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { visibleSource, removedSource });
        historyEventRepository
            .Setup(repository => repository.GetPublishedArticlesByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(
                    eventIds => eventIds.Count == 2
                        && eventIds.Contains(visibleNarrative.Id)
                        && eventIds.Contains(removedNarrative.Id)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { visibleNarrative, removedNarrative });
        PublicParkHistoricalDataLoader loader = CreateLoader(
            parkRepository,
            parkItemRepository,
            parkZoneRepository,
            factRepository);
        GetPublicParkHistoricalTimelineQueryHandler handler = new(
            loader,
            sourceRepository.Object,
            historyEventRepository.Object);

        ApplicationResult<PublicParkHistoricalTimelineResult> result = await handler.HandleAsync(
            new GetPublicParkHistoricalTimelineQuery("park-1", 2, 2));

        Assert.True(result.IsSuccess);
        PublicParkHistoricalTimelineResult timeline = Assert.IsType<PublicParkHistoricalTimelineResult>(
            result.Value);
        Assert.Equal(4, timeline.Page.TotalItems);
        Assert.Equal(2, timeline.Page.Items.Count);
        PublicHistoricalTimelineEntryResult visibleEntry = Assert.Single(
            timeline.Page.Items,
            entry => entry.Fact.Id == visibleFact.Id);
        PublicHistoricalTimelineEntryResult removedEntry = Assert.Single(
            timeline.Page.Items,
            entry => entry.Fact.Id == removedFact.Id);
        Assert.Equal(visibleSource.Id, Assert.Single(visibleEntry.Sources).Id);
        Assert.Equal(removedSource.Id, Assert.Single(removedEntry.Sources).Id);
        Assert.Same(visibleNarrative, visibleEntry.Narrative);
        Assert.Equal(visibleItem.Name, visibleEntry.CurrentSubjectName);
        Assert.Null(removedEntry.Narrative);
        Assert.Null(removedEntry.CurrentSubjectName);
        sourceRepository.VerifyAll();
        historyEventRepository.VerifyAll();
    }

    [Fact]
    public async Task Snapshot_ZoneNames_ExcludeHiddenCurrentZoneAndIncludeHistoricalOnlyZone()
    {
        Park park = PublicParkHistoryTestData.CreatePark();
        ParkZone visibleZone = PublicParkHistoryTestData.CreateParkZone("visible-zone", "Zone publique");
        ParkZone hiddenZone = PublicParkHistoryTestData.CreateParkZone(
            "hidden-zone",
            "Zone privée",
            false);
        HistoricalFact removedZoneFact = PublicParkHistoryTestData.CreateOpeningFact(
            new HistoricalSubject(
                HistoricalSubjectType.ParkZone,
                "removed-zone",
                "Zone historique publiée",
                HistoricalSubjectPublicationPolicy.HistoricalOnly,
                park.Id),
            1985);
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
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());
        parkZoneRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { visibleZone, hiddenZone });
        factRepository
            .Setup(repository => repository.GetLatestDecisionEligibleRevisionsForParkAsync(
                "park-1",
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { removedZoneFact });
        PublicParkHistoricalDataLoader loader = CreateLoader(
            parkRepository,
            parkItemRepository,
            parkZoneRepository,
            factRepository);
        GetPublicParkHistoricalSnapshotQueryHandler handler = new(
            loader,
            new ParkHistoricalSnapshotBuilder());

        ApplicationResult<PublicParkHistoricalSnapshotResult> result = await handler.HandleAsync(
            new GetPublicParkHistoricalSnapshotQuery("park-1", 1986, null, null));

        Assert.True(result.IsSuccess);
        PublicParkHistoricalSnapshotResult snapshot = Assert.IsType<PublicParkHistoricalSnapshotResult>(result.Value);
        Assert.Equal("Zone publique", snapshot.ZoneNames["visible-zone"]);
        Assert.Equal("Zone historique publiée", snapshot.ZoneNames["removed-zone"]);
        Assert.DoesNotContain("hidden-zone", snapshot.ZoneNames.Keys);
        Assert.DoesNotContain("Zone privée", snapshot.ZoneNames.Values);
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
            new Mock<IHistoricalSourceRepository>(MockBehavior.Strict).Object,
            new Mock<IHistoryEventRepository>(MockBehavior.Strict).Object);

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
            new Mock<IHistoricalSourceRepository>(MockBehavior.Strict).Object,
            new Mock<IHistoryEventRepository>(MockBehavior.Strict).Object);

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
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());
        parkZoneRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkZone>());
        factRepository
            .Setup(repository => repository.GetLatestDecisionEligibleRevisionsForParkPageAsync(
                "park-1",
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                int.MaxValue,
                50,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<HistoricalFact>(
                Array.Empty<HistoricalFact>(),
                int.MaxValue,
                50,
                1));
        PublicParkHistoricalDataLoader loader = CreateLoader(
            parkRepository,
            parkItemRepository,
            parkZoneRepository,
            factRepository);
        GetPublicParkHistoricalTimelineQueryHandler handler = new(
            loader,
            sourceRepository.Object,
            new Mock<IHistoryEventRepository>(MockBehavior.Strict).Object);

        ApplicationResult<PublicParkHistoricalTimelineResult> result = await handler.HandleAsync(
            new GetPublicParkHistoricalTimelineQuery("park-1", int.MaxValue, 50));

        Assert.True(result.IsSuccess);
        Assert.Empty(Assert.IsType<PublicParkHistoricalTimelineResult>(result.Value).Page.Items);
        sourceRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Snapshot_IncludesDeletedHistoricalOnlySubjectFromItsDurableParkScope()
    {
        Park park = PublicParkHistoryTestData.CreatePark();
        HistoricalFact deletedItemFact = PublicParkHistoryTestData.CreateOpeningFact(
            new HistoricalSubject(
                HistoricalSubjectType.ParkItem,
                "deleted-item",
                "Attraction disparue",
                HistoricalSubjectPublicationPolicy.HistoricalOnly,
                park.Id),
            1980);
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
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());
        parkZoneRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkZone>());
        factRepository
            .Setup(repository => repository.GetLatestDecisionEligibleRevisionsForParkAsync(
                "park-1",
                It.Is<IReadOnlyCollection<HistoricalSubject>>(subjects => subjects.Count == 1),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { deletedItemFact });
        PublicParkHistoricalDataLoader loader = CreateLoader(
            parkRepository,
            parkItemRepository,
            parkZoneRepository,
            factRepository);
        GetPublicParkHistoricalSnapshotQueryHandler handler = new(
            loader,
            new ParkHistoricalSnapshotBuilder());

        ApplicationResult<PublicParkHistoricalSnapshotResult> result = await handler.HandleAsync(
            new GetPublicParkHistoricalSnapshotQuery("park-1", 1981, null, null));

        Assert.True(result.IsSuccess);
        PublicParkHistoricalSnapshotResult snapshot = Assert.IsType<PublicParkHistoricalSnapshotResult>(result.Value);
        Assert.Contains(
            snapshot.Snapshot.Subjects,
            subject => subject.Subject.Id == "deleted-item"
                && subject.Subject.HistoricalLabel == "Attraction disparue");
        Assert.Equal(deletedItemFact.Id, Assert.Single(snapshot.Facts).Id);
    }

    [Fact]
    public async Task Snapshot_DoesNotAttachForeignHistoricalOnlyFactToReusedCurrentId()
    {
        Park park = PublicParkHistoryTestData.CreatePark();
        ParkItem reusedVisibleItem = PublicParkHistoryTestData.CreateParkItem(
            "reused-item",
            "Nouvelle attraction");
        HistoricalFact foreignHistoricalFact = PublicParkHistoryTestData.CreateOpeningFact(
            new HistoricalSubject(
                HistoricalSubjectType.ParkItem,
                reusedVisibleItem.Id,
                "Ancienne attraction étrangère",
                HistoricalSubjectPublicationPolicy.HistoricalOnly,
                "another-park"),
            1975);
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
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { reusedVisibleItem });
        parkZoneRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkZone>());
        factRepository
            .Setup(repository => repository.GetLatestDecisionEligibleRevisionsForParkAsync(
                "park-1",
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { foreignHistoricalFact });
        PublicParkHistoricalDataLoader loader = CreateLoader(
            parkRepository,
            parkItemRepository,
            parkZoneRepository,
            factRepository);
        GetPublicParkHistoricalSnapshotQueryHandler handler = new(
            loader,
            new ParkHistoricalSnapshotBuilder());

        ApplicationResult<PublicParkHistoricalSnapshotResult> result = await handler.HandleAsync(
            new GetPublicParkHistoricalSnapshotQuery("park-1", 1976, null, null));

        Assert.True(result.IsSuccess);
        PublicParkHistoricalSnapshotResult snapshot = Assert.IsType<PublicParkHistoricalSnapshotResult>(result.Value);
        Assert.Empty(snapshot.Facts);
        Assert.Contains(
            snapshot.Snapshot.Subjects,
            subject => subject.Subject.Id == reusedVisibleItem.Id
                && subject.Subject.PublicationPolicy
                    == HistoricalSubjectPublicationPolicy.FollowCurrentSubject);
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
