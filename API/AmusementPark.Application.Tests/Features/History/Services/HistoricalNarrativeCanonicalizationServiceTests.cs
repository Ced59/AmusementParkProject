using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Services;

public sealed class HistoricalNarrativeCanonicalizationServiceTests
{
    [Fact]
    public async Task CanonicalizeAsync_WhenNewNarrativeIsVisible_ShouldCreateOnlyAnOrdinaryDraft()
    {
        Mock<IHistoricalFactRepository> factRepository = new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository = new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = CreatePublicParkRepository();
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        List<HistoricalFact> writtenFacts = new List<HistoricalFact>();
        factRepository
            .Setup(repository => repository.AppendRevisionAsync(
                It.IsAny<HistoricalFact>(),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback<HistoricalFact, HistoricalReviewEvent, CancellationToken>(
                (fact, _, _) => writtenFacts.Add(fact))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        HistoricalNarrativeCanonicalizationService service = CreateService(
            factRepository.Object,
            sourceRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object);

        HistoricalNarrativeCanonicalizationResult result = await service.CanonicalizeAsync(
            CreateOpeningEvent(withSource: false),
            CancellationToken.None);

        Assert.Equal(HistoricalNarrativeCanonicalizationState.Canonicalized, result.State);
        Assert.NotNull(result.CanonicalFactId);
        HistoricalFact fact = Assert.Single(writtenFacts);
        Assert.Equal(HistoricalEditorialWorkflowState.Draft, fact.WorkflowState);
        Assert.Equal(HistoricalPublicationState.Draft, fact.PublicationState);
        Assert.Equal(HistoricalRevisionOrigin.Ordinary, fact.RevisionOrigin);
        Assert.Empty(fact.SourceReferences);
        sourceRepository.VerifyNoOtherCalls();
        factRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanonicalizeAsync_WhenVisibleNarrativeHasSource_ShouldCreateOnlyCanonicalPublishedRevisions()
    {
        Mock<IHistoricalFactRepository> factRepository = new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository = new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = CreatePublicParkRepository();
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        List<HistoricalFact> writtenFacts = new List<HistoricalFact>();
        List<HistoricalSourceReference> writtenSources = new List<HistoricalSourceReference>();
        factRepository
            .Setup(repository => repository.AppendRevisionAsync(
                It.IsAny<HistoricalFact>(),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback<HistoricalFact, HistoricalReviewEvent, CancellationToken>(
                (fact, _, _) => writtenFacts.Add(fact))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        sourceRepository
            .Setup(repository => repository.AppendRevisionAsync(
                It.IsAny<HistoricalSourceReference>(),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback<HistoricalSourceReference, HistoricalReviewEvent, CancellationToken>(
                (source, _, _) => writtenSources.Add(source))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        HistoricalNarrativeCanonicalizationService service = CreateService(
            factRepository.Object,
            sourceRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object);

        HistoricalNarrativeCanonicalizationResult result = await service.CanonicalizeAsync(
            CreateOpeningEvent(withSource: true),
            CancellationToken.None);

        Assert.Equal(HistoricalNarrativeCanonicalizationState.Canonicalized, result.State);
        Assert.All(writtenFacts, static fact => Assert.Equal(HistoricalRevisionOrigin.Ordinary, fact.RevisionOrigin));
        Assert.All(writtenSources, static source => Assert.Equal(HistoricalRevisionOrigin.Ordinary, source.RevisionOrigin));
        HistoricalFact publishedFact = Assert.Single(
            writtenFacts,
            static fact => fact.PublicationState == HistoricalPublicationState.Published);
        Assert.Equal(HistoricalFactState.Probable, publishedFact.State);
        Assert.Equal(5, publishedFact.Revision);
        Assert.Equal(8, publishedFact.PublicUncertaintyExplanation.Count);
        HistoricalSourceReference publishedSource = Assert.Single(
            writtenSources,
            static source => source.PublicationState == HistoricalPublicationState.Published);
        Assert.Equal(4, publishedSource.Revision);
        Assert.Contains(
            publishedFact.SourceReferences,
            reference => reference.SourceId == publishedSource.Id && reference.Revision == publishedSource.Revision);
        factRepository.VerifyAll();
        sourceRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CanonicalizeAsync_WhenPublishedFactCommitsWithoutAcknowledgement_ShouldReconcileExactRevision()
    {
        Mock<IHistoricalFactRepository> factRepository = new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository = new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = CreatePublicParkRepository();
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        factRepository
            .Setup(repository => repository.AppendRevisionAsync(
                It.Is<HistoricalFact>(fact => fact.Revision != 5),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        factRepository
            .SetupSequence(repository => repository.AppendRevisionAsync(
                It.Is<HistoricalFact>(fact => fact.Revision == 5
                    && fact.PublicationState == HistoricalPublicationState.Published),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("acknowledgement lost after commit"))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.AlreadyExists);
        sourceRepository
            .Setup(repository => repository.AppendRevisionAsync(
                It.IsAny<HistoricalSourceReference>(),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        HistoricalNarrativeCanonicalizationService service = CreateService(
            factRepository.Object,
            sourceRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object);

        HistoricalNarrativeCanonicalizationResult result = await service.CanonicalizeAsync(
            CreateOpeningEvent(withSource: true),
            CancellationToken.None);

        Assert.Equal(HistoricalNarrativeCanonicalizationState.Canonicalized, result.State);
        Assert.NotNull(result.CanonicalFactId);
        factRepository.Verify(repository => repository.AppendRevisionAsync(
            It.Is<HistoricalFact>(fact => fact.Revision == 5),
            It.IsAny<HistoricalReviewEvent>(),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
        factRepository.VerifyAll();
        sourceRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MigrateExistingAsync_WhenSourceUrlExceedsDomainLimit_ShouldKeepDraftAndReportWarning()
    {
        Mock<IHistoricalFactRepository> factRepository =
            new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository =
            new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = CreatePublicParkRepository();
        Mock<IParkItemRepository> parkItemRepository =
            new Mock<IParkItemRepository>(MockBehavior.Strict);
        List<HistoricalFact> writtenFacts = new List<HistoricalFact>();
        factRepository
            .Setup(repository => repository.AppendRevisionAsync(
                It.IsAny<HistoricalFact>(),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback<HistoricalFact, HistoricalReviewEvent, CancellationToken>(
                (fact, _, _) => writtenFacts.Add(fact))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        HistoricalNarrativeCanonicalizationService service = CreateService(
            factRepository.Object,
            sourceRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object);
        HistoryEvent historyEvent = CreateOpeningEvent(withSource: false);
        historyEvent.Sources.Add(new HistorySourceReference
        {
            Label = "Source historique invalide",
            Url = $"https://example.com/{new string('a', 2000)}",
            AccessedAt = "2026-09-30",
        });

        HistoricalNarrativeCanonicalizationResult result = await service.MigrateExistingAsync(
            historyEvent,
            CancellationToken.None);

        Assert.Equal(HistoricalNarrativeCanonicalizationState.Canonicalized, result.State);
        Assert.Contains("history-canonicalization.invalid-source", result.Warnings);
        Assert.Contains(
            "history-canonicalization.publication-deferred-without-source",
            result.Warnings);
        HistoricalFact fact = Assert.Single(writtenFacts);
        Assert.Equal(HistoricalPublicationState.Draft, fact.PublicationState);
        Assert.Empty(fact.SourceReferences);
        sourceRepository.VerifyNoOtherCalls();
        factRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
    }

    private static Mock<IParkRepository> CreatePublicParkRepository()
    {
        Mock<IParkRepository> repository = new Mock<IParkRepository>(MockBehavior.Strict);
        repository
            .Setup(candidate => candidate.GetByIdAsync(
                "park-1",
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park
            {
                Id = "park-1",
                Name = "Parc de référence",
                IsVisible = true,
                Status = ParkStatus.Operating,
                AdminReviewStatus = AdminReviewStatus.Validated,
            });
        return repository;
    }

    private static HistoricalNarrativeCanonicalizationService CreateService(
        IHistoricalFactRepository factRepository,
        IHistoricalSourceRepository sourceRepository,
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository)
    {
        HistoricalNarrativeCanonicalSourcePlanner sourcePlanner =
            new HistoricalNarrativeCanonicalSourcePlanner();
        return new HistoricalNarrativeCanonicalizationService(
            new HistoricalNarrativeCanonicalSubjectResolver(parkRepository, parkItemRepository),
            sourcePlanner,
            new HistoricalNarrativeCanonicalFactFactory(),
            new HistoricalNarrativeCanonicalRevisionWriter(
                factRepository,
                sourceRepository,
                sourcePlanner));
    }

    private static HistoryEvent CreateOpeningEvent(bool withSource)
    {
        DateTime recordedAtUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        HistoryEvent historyEvent = new HistoryEvent
        {
            Id = "history-1",
            Key = "park-1-opening",
            EntityType = HistoryEntityType.Park,
            OwnerId = "park-1",
            ParkId = "park-1",
            Year = 2001,
            DatePrecision = HistoryDatePrecision.Year,
            EventType = ParkHistoryEventType.Opening.ToString(),
            IsMajor = true,
            IsVisible = true,
            CreatedAtUtc = recordedAtUtc,
            UpdatedAtUtc = recordedAtUtc,
        };
        if (withSource)
        {
            historyEvent.Sources.Add(new HistorySourceReference
            {
                Label = "Site officiel",
                Url = "https://example.com/history",
                AccessedAt = "2026-09-30",
            });
        }

        return historyEvent;
    }
}
