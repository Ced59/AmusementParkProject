using AmusementPark.Application.Errors;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Queries;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.History.Services;
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
    public async Task Comparison_WhenRangeIsReversed_ReturnsValidationWithoutReadingData()
    {
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        PublicParkHistoricalDataLoader loader = CreateLoader(
            parkRepository,
            new Mock<IParkItemRepository>(MockBehavior.Strict),
            new Mock<IParkZoneRepository>(MockBehavior.Strict),
            new Mock<IHistoricalFactRepository>(MockBehavior.Strict));
        GetPublicParkHistoricalComparisonQueryHandler handler = new(
            loader,
            new ParkHistoricalSnapshotBuilder(),
            new ParkHistoricalComparisonBuilder());

        ApplicationResult<PublicParkHistoricalComparisonResult> result = await handler.HandleAsync(
            new GetPublicParkHistoricalComparisonQuery("park-1", 2026, 1998));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "history.comparison.range.invalid");
        parkRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Comparison_LoadsPublicScopeOnceAndBuildsBothSnapshots()
    {
        Park park = PublicParkHistoryTestData.CreatePark();
        ParkItem item = PublicParkHistoryTestData.CreateParkItem("item-1", "Attraction témoin");
        HistoricalFact opening = PublicParkHistoryTestData.CreateOpeningFact(
            new HistoricalSubject(
                HistoricalSubjectType.ParkItem,
                item.Id,
                item.Name,
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                park.Id),
            2010);
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new(MockBehavior.Strict);
        Mock<IParkZoneRepository> parkZoneRepository = new(MockBehavior.Strict);
        Mock<IHistoricalFactRepository> factRepository = new(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkItemRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });
        parkZoneRepository
            .Setup(repository => repository.GetByParkIdAsync("park-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkZone>());
        factRepository
            .Setup(repository => repository.GetLatestDecisionEligibleRevisionsForParkAsync(
                "park-1",
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { opening });
        GetPublicParkHistoricalComparisonQueryHandler handler = new(
            CreateLoader(parkRepository, parkItemRepository, parkZoneRepository, factRepository),
            new ParkHistoricalSnapshotBuilder(),
            new ParkHistoricalComparisonBuilder());

        ApplicationResult<PublicParkHistoricalComparisonResult> result = await handler.HandleAsync(
            new GetPublicParkHistoricalComparisonQuery("park-1", 1998, 2026));

        Assert.True(result.IsSuccess);
        PublicParkHistoricalComparisonResult comparison =
            Assert.IsType<PublicParkHistoricalComparisonResult>(result.Value);
        Assert.Equal(1998, comparison.Comparison.From.RequestedInstant.Year);
        Assert.Equal(2026, comparison.Comparison.To.RequestedInstant.Year);
        Assert.Equal(
            HistoricalPresenceChange.Opened,
            comparison.Comparison.Subjects.Single(subject => subject.To.Subject.Id == item.Id).PresenceChange);
        parkRepository.VerifyAll();
        parkItemRepository.VerifyAll();
        parkZoneRepository.VerifyAll();
        factRepository.VerifyAll();
    }

    [Fact]
    public async Task Timeline_WhenParkRolloutGateIsClosed_ShouldRemainUnavailable()
    {
        Park park = PublicParkHistoryTestData.CreatePark();
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new(MockBehavior.Strict);
        Mock<IParkZoneRepository> parkZoneRepository = new(MockBehavior.Strict);
        Mock<IHistoricalFactRepository> factRepository = new(MockBehavior.Strict);
        parkRepository.Setup(repository => repository.GetByIdAsync(
                park.Id,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        parkItemRepository.Setup(repository => repository.GetByParkIdAsync(
                park.Id,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkItem>());
        parkZoneRepository.Setup(repository => repository.GetByParkIdAsync(
                park.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkZone>());
        factRepository.Setup(repository => repository.GetLatestDecisionEligibleRevisionsForParkAsync(
                park.Id,
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<HistoricalFact>());
        GetPublicParkHistoricalTimelineQueryHandler handler = new(
            CreateLoader(
                parkRepository,
                parkItemRepository,
                parkZoneRepository,
                factRepository,
                false),
            new Mock<IHistoricalSourceRepository>(MockBehavior.Strict).Object,
            new Mock<IHistoryEventRepository>(MockBehavior.Strict).Object);

        ApplicationResult<PublicParkHistoricalTimelineResult> result = await handler.HandleAsync(
            new GetPublicParkHistoricalTimelineQuery(park.Id));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "park.not-found");
    }

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
        HistoricalFact earlierFact = PublicParkHistoryTestData.CreateOpeningFact(
            removedSubject,
            1970);
        HistoricalFact laterEarlierFact = PublicParkHistoryTestData.CreateOpeningFact(
            removedSubject,
            1980);
        HistoricalSourceReference visibleSource = PublicParkHistoryTestData.CreateSource(visibleFact);
        HistoricalSourceReference removedSource = PublicParkHistoryTestData.CreateSource(removedFact);
        HistoricalRelation visibleRelation = CreatePublishedRelation(visibleSubject);
        HistoricalSourceReference relationSource = CreateRelationSource(visibleRelation);
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
        Mock<IHistoricalRelationRepository> relationRepository = new(MockBehavior.Strict);
        Mock<IHistoricalSubjectPublicationStateReader> publicationReader = new(MockBehavior.Strict);
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
            .Setup(repository => repository.GetLatestDecisionEligibleRevisionsForParkAsync(
                "park-1",
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { visibleFact, removedFact, earlierFact, laterEarlierFact });
        factRepository
            .Setup(repository => repository.GetLatestPublicTimelineRevisionsForParkPageAsync(
                "park-1",
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                2,
                2,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<HistoricalFact>(
                new[] { visibleFact, removedFact },
                2,
                2,
                4));
        sourceRepository
            .Setup(repository => repository.GetRevisionsAsync(
                It.Is<IReadOnlyCollection<HistoricalSourceRevisionReference>>(
                    references => references.Count == 2
                        && references.Any(reference => reference.SourceId == visibleSource.Id)
                        && references.Any(reference => reference.SourceId == removedSource.Id)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { visibleSource, removedSource });
        sourceRepository
            .Setup(repository => repository.GetRevisionsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalRelationSourceRevisionReference>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { relationSource });
        sourceRepository
            .Setup(repository => repository.GetLatestRevisionsAsync(
                It.Is<IReadOnlyCollection<Guid>>(ids => ids.Count == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { visibleSource, removedSource });
        sourceRepository
            .Setup(repository => repository.GetLatestRevisionsAsync(
                It.Is<IReadOnlyCollection<Guid>>(
                    ids => ids.Count == 1 && ids.Contains(relationSource.Id)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { relationSource });
        historyEventRepository
            .Setup(repository => repository.GetPublishedArticlesByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(
                    eventIds => eventIds.Count == 2
                        && eventIds.Contains(visibleNarrative.Id)
                        && eventIds.Contains(removedNarrative.Id)),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { visibleNarrative, removedNarrative });
        relationRepository
            .Setup(repository => repository.GetLatestDecisionEligibleRevisionsTouchingEachSubjectAsync(
                It.Is<IReadOnlyCollection<HistoricalSubjectKey>>(keys => keys.Count == 2),
                200,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { visibleRelation });
        publicationReader
            .Setup(reader => reader.GetPublicSubjectKeysAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<HistoricalSubjectKey>
            {
                new HistoricalSubjectKey(
                    visibleSubject.Type,
                    visibleSubject.Id,
                    visibleSubject.ContextParkId),
                new HistoricalSubjectKey(
                    visibleRelation.Target.Type,
                    visibleRelation.Target.Id,
                    visibleRelation.Target.ContextParkId),
            });
        PublicParkHistoricalDataLoader loader = CreateLoader(
            parkRepository,
            parkItemRepository,
            parkZoneRepository,
            factRepository);
        GetPublicParkHistoricalTimelineQueryHandler handler = new(
            loader,
            sourceRepository.Object,
            historyEventRepository.Object,
            relationRepository.Object,
            publicationReader.Object);

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
        Assert.True(visibleEntry.HasPublishedLineage);
        Assert.Null(removedEntry.Narrative);
        Assert.Null(removedEntry.CurrentSubjectName);
        Assert.False(removedEntry.HasPublishedLineage);
        sourceRepository.VerifyAll();
        historyEventRepository.VerifyAll();
        relationRepository.VerifyAll();
        publicationReader.VerifyAll();
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
        HistoricalFact parkFact = PublicParkHistoryTestData.CreateOpeningFact(
            new HistoricalSubject(
                HistoricalSubjectType.Park,
                park.Id,
                park.Name!,
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                park.Id),
            1998);
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
            .Setup(repository => repository.GetLatestDecisionEligibleRevisionsForParkAsync(
                "park-1",
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { parkFact });
        factRepository
            .Setup(repository => repository.GetLatestPublicTimelineRevisionsForParkPageAsync(
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
    public async Task Snapshot_DoesNotAttachFormerParkFactToMovedCurrentSubject()
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
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
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
        Mock<IHistoricalFactRepository> factRepository,
        bool rolloutIsOpen = true)
    {
        Mock<IHistoricalParkRolloutGateAssessmentService> rolloutGate = new(MockBehavior.Strict);
        rolloutGate.Setup(service => service.AssessAsync(
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<IReadOnlyCollection<HistoricalFact>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rolloutIsOpen
                ? new HistoricalParkRolloutGate(2, 2, 1, new[] { 1998 })
                : new HistoricalParkRolloutGate(0, 0, 0, Array.Empty<int>()));
        return new PublicParkHistoricalDataLoader(
            parkRepository.Object,
            parkItemRepository.Object,
            parkZoneRepository.Object,
            factRepository.Object,
            rolloutGate.Object);
    }

    private static HistoricalFact CreateLegacyPublishedFact(Park park)
    {
        return new HistoricalFact(
            Guid.NewGuid(),
            new HistoricalSubject(
                HistoricalSubjectType.Park,
                park.Id,
                park.Name!,
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                park.Id),
            HistoricalFactType.Opening,
            HistoricalPeriod.Point(HistoricalDate.ForYear(1967)),
            HistoricalFactState.Unverified,
            HistoricalImportance.Major,
            HistoricalEditorialWorkflowState.EditorialReview,
            HistoricalPublicationState.LegacyPublishedPendingReview,
            HistoricalLocalizationPolicy.SupportedLanguageCodes
                .Select(static languageCode => new HistoricalLocalizedText(
                    languageCode,
                    "Cette information historique publique reste à vérifier."))
                .ToArray(),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null,
            null,
            Array.Empty<HistoricalSourceRevisionReference>(),
            null,
            null,
            null,
            null,
            null,
            "hist-v1-legacy",
            1,
            null,
            new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc),
            HistoricalRevisionOrigin.LegacyMigration);
    }

    private static HistoricalRelation CreatePublishedRelation(HistoricalSubject source)
    {
        HistoricalSubject target = new(
            HistoricalSubjectType.ParkItem,
            "successor-item",
            "Attraction suivante",
            HistoricalSubjectPublicationPolicy.HistoricalOnly,
            source.ContextParkId);
        HistoricalPeriod period = HistoricalPeriod.Point(HistoricalDate.ForYear(2011));
        Guid sourceId = Guid.NewGuid();
        return new HistoricalRelation(
            Guid.NewGuid(),
            source,
            target,
            HistoricalRelationType.ReplacedBy,
            HistoricalRelationDirection.Directed,
            period,
            HistoricalFactState.Verified,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            Array.Empty<HistoricalLocalizedText>(),
            new[]
            {
                new HistoricalRelationSourceRevisionReference(
                    sourceId,
                    2,
                    new HistoricalSubjectKey(source.Type, source.Id, source.ContextParkId),
                    new HistoricalSubjectKey(target.Type, target.Id, target.ContextParkId),
                    HistoricalRelationType.ReplacedBy,
                    period,
                    HistoricalEvidencePosition.Supports,
                    new[]
                    {
                        HistoricalSourceScope.RelationSourceIdentity,
                        HistoricalSourceScope.RelationTargetIdentity,
                        HistoricalSourceScope.RelationType,
                        HistoricalSourceScope.Period,
                    }),
            },
            null,
            new DateTime(2026, 9, 27, 9, 58, 0, DateTimeKind.Utc),
            new DateTime(2026, 9, 27, 9, 59, 0, DateTimeKind.Utc),
            "hist-v1",
            2,
            1,
            new DateTime(2026, 9, 27, 10, 0, 0, DateTimeKind.Utc));
    }

    private static HistoricalSourceReference CreateRelationSource(HistoricalRelation relation)
    {
        HistoricalRelationSourceRevisionReference reference = Assert.Single(relation.SourceReferences);
        return new HistoricalSourceReference(
            reference.SourceId,
            reference.Revision,
            HistoricalSourceType.OfficialWebsite,
            "Historique officiel",
            "Parc exemple",
            "https://example.com/history",
            null,
            new DateOnly(2011, 1, 1),
            new DateOnly(2026, 9, 27),
            "fr",
            null,
            reference.Scopes,
            null,
            HistoricalSourceAccessibility.Accessible,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            new DateTime(2026, 9, 27, 10, 0, 0, DateTimeKind.Utc));
    }
}
