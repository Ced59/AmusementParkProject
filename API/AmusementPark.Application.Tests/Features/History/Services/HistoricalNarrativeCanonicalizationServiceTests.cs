using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Tests.Features.History.Handlers;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Services;

public sealed class HistoricalNarrativeCanonicalizationServiceTests
{
    [Fact]
    public async Task NeedsCanonicalRepairAsync_WhenLinkedFactWasRemoved_ShouldRequestRepair()
    {
        Guid factId = Guid.NewGuid();
        Mock<IHistoricalFactRepository> factRepository = new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository = new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        factRepository
            .Setup(repository => repository.GetLatestRevisionAsync(
                factId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((HistoricalFact?)null);
        HistoricalNarrativeCanonicalizationService service = CreateService(
            factRepository.Object,
            sourceRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object);
        HistoryEvent historyEvent = CreateOpeningEvent(withSource: true);
        historyEvent.CanonicalFactId = factId;
        historyEvent.CanonicalizationState = HistoricalNarrativeCanonicalizationState.Canonicalized;

        bool repairRequired = await service.NeedsCanonicalRepairAsync(
            historyEvent,
            CancellationToken.None);

        Assert.True(repairRequired);
        factRepository.VerifyAll();
        sourceRepository.VerifyNoOtherCalls();
        parkRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NeedsCanonicalRepairAsync_WhenLinkedFactExistsOnlyAsDraft_ShouldRequestRepair()
    {
        Guid factId = Guid.NewGuid();
        Mock<IHistoricalFactRepository> factRepository = new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository = new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = CreatePublicParkRepository();
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        factRepository
            .Setup(repository => repository.GetLatestRevisionAsync(
                factId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateExistingCanonicalFact(factId));
        HistoricalNarrativeCanonicalizationService service = CreateService(
            factRepository.Object,
            sourceRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object);
        HistoryEvent historyEvent = CreateOpeningEvent(withSource: true);
        historyEvent.CanonicalFactId = factId;
        historyEvent.CanonicalizationState = HistoricalNarrativeCanonicalizationState.Canonicalized;

        bool repairRequired = await service.NeedsCanonicalRepairAsync(
            historyEvent,
            CancellationToken.None);

        Assert.True(repairRequired);
        factRepository.VerifyAll();
        sourceRepository.VerifyNoOtherCalls();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NeedsCanonicalRepairAsync_WhenPublishedFactLostItsSource_ShouldRequestRepair()
    {
        Guid factId = Guid.NewGuid();
        HistoricalFact fact = CreatePublishedCanonicalFact(factId);
        Mock<IHistoricalFactRepository> factRepository = new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository = new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = CreatePublicParkRepository();
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        factRepository
            .Setup(repository => repository.GetLatestRevisionAsync(
                factId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fact);
        sourceRepository
            .Setup(repository => repository.GetRevisionsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSourceRevisionReference>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<HistoricalSourceReference>());
        HistoricalNarrativeCanonicalizationService service = CreateService(
            factRepository.Object,
            sourceRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object);
        HistoryEvent historyEvent = CreateOpeningEvent(withSource: true);
        historyEvent.CanonicalFactId = factId;

        bool repairRequired = await service.NeedsCanonicalRepairAsync(
            historyEvent,
            CancellationToken.None);

        Assert.True(repairRequired);
        factRepository.VerifyAll();
        sourceRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NeedsCanonicalRepairAsync_WhenPublishedFactHasPublicEvidence_ShouldNotRequestRepair()
    {
        Guid factId = Guid.NewGuid();
        HistoricalFact fact = CreatePublishedCanonicalFact(factId);
        HistoricalSourceReference source = PublicParkHistoryTestData.CreateSource(fact);
        Mock<IHistoricalFactRepository> factRepository = new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository = new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = CreatePublicParkRepository();
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        factRepository
            .Setup(repository => repository.GetLatestRevisionAsync(
                factId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fact);
        factRepository
            .Setup(repository => repository.IsLatestRevisionSubjectAlignedAsync(
                factId,
                It.IsAny<HistoricalSubject>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        sourceRepository
            .Setup(repository => repository.GetRevisionsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSourceRevisionReference>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { source });
        sourceRepository
            .Setup(repository => repository.GetLatestRevisionsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { source });
        HistoricalNarrativeCanonicalizationService service = CreateService(
            factRepository.Object,
            sourceRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object);
        HistoryEvent historyEvent = CreateOpeningEvent(withSource: true);
        historyEvent.CanonicalFactId = factId;

        bool repairRequired = await service.NeedsCanonicalRepairAsync(
            historyEvent,
            CancellationToken.None);

        Assert.False(repairRequired);
        factRepository.VerifyAll();
        sourceRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NeedsCanonicalRepairAsync_WhenFactWasExplicitlyWithdrawn_ShouldPreserveWithdrawal()
    {
        Guid factId = Guid.NewGuid();
        HistoricalFact fact = CreatePublishedCanonicalFact(factId)
            .CreateRetraction(new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc));
        Mock<IHistoricalFactRepository> factRepository = new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository = new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        factRepository
            .Setup(repository => repository.GetLatestRevisionAsync(
                factId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fact);
        factRepository
            .Setup(repository => repository.WasLatestRevisionTransitionRecordedByAsync(
                factId,
                HistoricalReviewEventType.Retracted,
                "system:history-narrative-change",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        HistoricalNarrativeCanonicalizationService service = CreateService(
            factRepository.Object,
            sourceRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object);
        HistoryEvent historyEvent = CreateOpeningEvent(withSource: true);
        historyEvent.CanonicalFactId = factId;

        bool repairRequired = await service.NeedsCanonicalRepairAsync(
            historyEvent,
            CancellationToken.None);

        Assert.False(repairRequired);
        factRepository.VerifyAll();
        sourceRepository.VerifyNoOtherCalls();
        parkRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NeedsCanonicalRepairAsync_WhenFactWasAutomaticallyRetracted_ShouldRequestRepair()
    {
        Guid factId = Guid.NewGuid();
        HistoricalFact fact = CreatePublishedCanonicalFact(factId)
            .CreateRetraction(new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc));
        HistoricalSourceReference source = PublicParkHistoryTestData.CreateSource(fact);
        Mock<IHistoricalFactRepository> factRepository =
            new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository =
            new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository =
            new Mock<IParkItemRepository>(MockBehavior.Strict);
        factRepository
            .Setup(repository => repository.GetLatestRevisionAsync(
                factId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fact);
        factRepository
            .Setup(repository => repository.WasLatestRevisionTransitionRecordedByAsync(
                factId,
                HistoricalReviewEventType.Retracted,
                "system:history-narrative-change",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        sourceRepository
            .Setup(repository => repository.GetLatestRevisionsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { source });
        HistoricalNarrativeCanonicalizationService service = CreateService(
            factRepository.Object,
            sourceRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object);
        HistoryEvent historyEvent = CreateOpeningEvent(withSource: true);
        historyEvent.CanonicalFactId = factId;

        bool repairRequired = await service.NeedsCanonicalRepairAsync(
            historyEvent,
            CancellationToken.None);

        Assert.True(repairRequired);
        factRepository.VerifyAll();
        sourceRepository.VerifyAll();
        parkRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public async Task NeedsCanonicalRepairAsync_WhenLatestSourceWasWithdrawn_ShouldRespectRetractionOrigin(
        bool automaticallyRetracted,
        bool expectedRepair)
    {
        Guid factId = Guid.NewGuid();
        HistoricalFact fact = CreatePublishedCanonicalFact(factId);
        HistoricalSourceReference source = PublicParkHistoryTestData.CreateSource(fact);
        HistoricalSourceReference withdrawnSource = CreateWithdrawnSource(source);
        Mock<IHistoricalFactRepository> factRepository =
            new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository =
            new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = CreatePublicParkRepository();
        Mock<IParkItemRepository> parkItemRepository =
            new Mock<IParkItemRepository>(MockBehavior.Strict);
        factRepository
            .Setup(repository => repository.GetLatestRevisionAsync(
                factId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fact);
        sourceRepository
            .Setup(repository => repository.GetRevisionsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSourceRevisionReference>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { source });
        sourceRepository
            .Setup(repository => repository.GetLatestRevisionsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { withdrawnSource });
        sourceRepository
            .Setup(repository => repository.WasLatestRevisionTransitionRecordedByAsync(
                source.Id,
                HistoricalReviewEventType.Retracted,
                "system:history-canonicalization",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(automaticallyRetracted);
        HistoricalNarrativeCanonicalizationService service = CreateService(
            factRepository.Object,
            sourceRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object);
        HistoryEvent historyEvent = CreateOpeningEvent(withSource: true);
        historyEvent.CanonicalFactId = factId;

        bool repairRequired = await service.NeedsCanonicalRepairAsync(
            historyEvent,
            CancellationToken.None);

        Assert.Equal(expectedRepair, repairRequired);
        factRepository.VerifyAll();
        sourceRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NeedsCanonicalRepairAsync_WhenPublicNarrativeFactHasSuppressedSubject_ShouldRequestRepair()
    {
        Guid factId = Guid.NewGuid();
        HistoricalFact fact = CreateSuppressedCanonicalFact(factId);
        Mock<IHistoricalFactRepository> factRepository =
            new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository =
            new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = CreatePublicParkRepository();
        Mock<IParkItemRepository> parkItemRepository =
            new Mock<IParkItemRepository>(MockBehavior.Strict);
        factRepository
            .Setup(repository => repository.GetLatestRevisionAsync(
                factId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fact);
        HistoricalNarrativeCanonicalizationService service = CreateService(
            factRepository.Object,
            sourceRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object);
        HistoryEvent historyEvent = CreateOpeningEvent(withSource: true);
        historyEvent.CanonicalFactId = factId;

        bool repairRequired = await service.NeedsCanonicalRepairAsync(
            historyEvent,
            CancellationToken.None);

        Assert.True(repairRequired);
        factRepository.VerifyAll();
        sourceRepository.VerifyNoOtherCalls();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NeedsCanonicalRepairAsync_WhenPublicNarrativeFactLostParkContext_ShouldRequestRepair()
    {
        Guid factId = Guid.NewGuid();
        HistoricalFact fact = CreatePublishedCanonicalFact(
            factId,
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            null);
        HistoricalSourceReference source = PublicParkHistoryTestData.CreateSource(fact);
        Mock<IHistoricalFactRepository> factRepository =
            new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository =
            new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = CreatePublicParkRepository();
        Mock<IParkItemRepository> parkItemRepository =
            new Mock<IParkItemRepository>(MockBehavior.Strict);
        factRepository
            .Setup(repository => repository.GetLatestRevisionAsync(
                factId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fact);
        factRepository
            .Setup(repository => repository.IsLatestRevisionSubjectAlignedAsync(
                factId,
                It.IsAny<HistoricalSubject>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        sourceRepository
            .Setup(repository => repository.GetRevisionsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSourceRevisionReference>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { source });
        sourceRepository
            .Setup(repository => repository.GetLatestRevisionsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { source });
        HistoricalNarrativeCanonicalizationService service = CreateService(
            factRepository.Object,
            sourceRepository.Object,
            parkRepository.Object,
            parkItemRepository.Object);
        HistoryEvent historyEvent = CreateOpeningEvent(withSource: true);
        historyEvent.CanonicalFactId = factId;

        bool repairRequired = await service.NeedsCanonicalRepairAsync(
            historyEvent,
            CancellationToken.None);

        Assert.True(repairRequired);
        factRepository.VerifyAll();
        sourceRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
    }

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

    private static HistoricalFact CreateExistingCanonicalFact(Guid factId)
    {
        DateTime recordedAtUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        return new HistoricalFact(
            factId,
            new HistoricalSubject(
                HistoricalSubjectType.Park,
                "park-1",
                "Parc de référence",
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
                "park-1"),
            HistoricalFactType.Opening,
            HistoricalPeriod.Point(HistoricalDate.ForYear(2001)),
            HistoricalFactState.Unverified,
            HistoricalImportance.Major,
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft,
            Array.Empty<HistoricalLocalizedText>(),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null,
            null,
            Array.Empty<HistoricalSourceRevisionReference>(),
            null,
            null,
            "history-1",
            null,
            null,
            null,
            1,
            null,
            recordedAtUtc);
    }

    private static HistoricalFact CreatePublishedCanonicalFact(
        Guid factId,
        HistoricalSubjectPublicationPolicy publicationPolicy =
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
        string? contextParkId = "park-1")
    {
        return PublicParkHistoryTestData.CreateOpeningFact(
            new HistoricalSubject(
                HistoricalSubjectType.Park,
                "park-1",
                "Parc de référence",
                publicationPolicy,
                contextParkId),
            2001,
            factId,
            narrativeContentId: "history-1");
    }

    private static HistoricalFact CreateSuppressedCanonicalFact(Guid factId)
    {
        DateTime recordedAtUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
        return new HistoricalFact(
            factId,
            new HistoricalSubject(
                HistoricalSubjectType.Park,
                "park-1",
                "Parc de référence",
                HistoricalSubjectPublicationPolicy.Suppressed),
            HistoricalFactType.Opening,
            HistoricalPeriod.Point(HistoricalDate.ForYear(2001)),
            HistoricalFactState.Unverified,
            HistoricalImportance.Major,
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft,
            Array.Empty<HistoricalLocalizedText>(),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null,
            null,
            Array.Empty<HistoricalSourceRevisionReference>(),
            null,
            null,
            "history-1",
            null,
            null,
            null,
            1,
            null,
            recordedAtUtc,
            HistoricalRevisionOrigin.Ordinary);
    }

    private static HistoricalSourceReference CreateWithdrawnSource(
        HistoricalSourceReference source)
    {
        return new HistoricalSourceReference(
            source.Id,
            source.Revision + 1,
            source.Type,
            source.Title,
            source.PublisherOrAuthor,
            source.Url,
            source.BibliographicReference,
            source.PublishedOn,
            source.AccessedOn,
            source.LanguageCode,
            source.ArchiveUrl,
            source.Scopes,
            source.AdminNote,
            HistoricalSourceAccessibility.Withdrawn,
            HistoricalEditorialWorkflowState.Retracted,
            HistoricalPublicationState.Withdrawn,
            source.RecordedAtUtc.AddMinutes(1),
            source.RevisionOrigin);
    }
}
