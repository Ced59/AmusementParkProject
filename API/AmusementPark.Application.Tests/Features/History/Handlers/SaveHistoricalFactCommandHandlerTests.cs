using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Commands;
using AmusementPark.Application.Features.History.Contracts;
using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Handlers;

public sealed class SaveHistoricalFactCommandHandlerTests
{
    private static readonly DateTimeOffset Now =
        new DateTimeOffset(2026, 9, 28, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_ShouldLoadAndPersistTheExactSelectedSourceRevision()
    {
        Park park = PublicParkHistoryTestData.CreatePark(false);
        ParkItem item = PublicParkHistoryTestData.CreateParkItem("item-1", "Attraction historique", false);
        HistoricalSourceReference selectedSource = CreateSource(2);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> zones = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IHistoricalFactRepository> facts = new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalRelationRepository> relations =
            new Mock<IHistoricalRelationRepository>(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sources =
            new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapRefreshScheduler> sitemapRefreshScheduler =
            new Mock<ISeoSitemapRefreshScheduler>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdAsync(
                park.Id,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        items.Setup(repository => repository.GetByParkIdAsync(
                park.Id,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });
        zones.Setup(repository => repository.GetByParkIdAsync(
                park.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkZone>());
        facts.Setup(repository => repository.GetLatestRevisionsForParkAsync(
                park.Id,
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<HistoricalFact>());
        relations.Setup(repository => repository.GetLatestRevisionsForParkAsync(
                park.Id,
                It.IsAny<IReadOnlyCollection<HistoricalSubjectKey>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<HistoricalRelation>());
        sources.Setup(repository => repository.GetRevisionsAsync(
                It.Is<IReadOnlyCollection<HistoricalSourceRevisionKey>>(revisions =>
                    revisions.Count == 1
                    && revisions.Single().SourceId == selectedSource.Id
                    && revisions.Single().Revision == selectedSource.Revision),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { selectedSource });
        facts.Setup(repository => repository.AppendRevisionAsync(
                It.Is<HistoricalFact>(fact =>
                    fact.SourceReferences.Count == 1
                    && fact.SourceReferences[0].SourceId == selectedSource.Id
                    && fact.SourceReferences[0].Revision == selectedSource.Revision),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        HistoricalParkEditorialScopeLoader scopeLoader = new HistoricalParkEditorialScopeLoader(
            parks.Object,
            items.Object,
            zones.Object);
        SaveHistoricalFactCommandHandler handler = new SaveHistoricalFactCommandHandler(
            scopeLoader,
            new HistoricalParkEditorialSubjectResolver(facts.Object, relations.Object),
            facts.Object,
            sources.Object,
            sitemapRefreshScheduler.Object,
            new FixedHistoryEditorialTimeProvider(Now));
        HistoricalPeriodInput period = new HistoricalPeriodInput(
            new HistoricalDateInput(2001, null, null, HistoryDatePrecision.Year, false, null),
            new HistoricalDateInput(2001, null, null, HistoryDatePrecision.Year, false, null),
            PeriodBoundaryConfidence.Confirmed,
            PeriodBoundaryConfidence.Confirmed);
        HistoricalFactDraftInput draft = new HistoricalFactDraftInput(
            HistoricalSubjectType.ParkItem,
            item.Id,
            HistoricalFactType.Opening,
            period,
            HistoricalFactState.Unverified,
            HistoricalImportance.Major,
            Array.Empty<HistoricalLocalizedText>(),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null,
            null,
            new[]
            {
                new HistoricalEvidenceSourceInput(
                    selectedSource.Id,
                    selectedSource.Revision,
                    HistoricalEvidencePosition.Supports),
            },
            null,
            null,
            null,
            item.ParkId);

        ApplicationResult<HistoricalEditorialMutationResult> result = await handler.HandleAsync(
            new SaveHistoricalFactCommand(
                park.Id,
                null,
                null,
                draft,
                "admin-1",
                "Création documentée"));

        Assert.True(result.IsSuccess);
        parks.VerifyAll();
        items.VerifyAll();
        zones.VerifyAll();
        facts.VerifyAll();
        relations.VerifyAll();
        sources.VerifyAll();
        sitemapRefreshScheduler.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenPublishedFactIsCorrected_ShouldRefreshSitemap()
    {
        Park park = PublicParkHistoryTestData.CreatePark(false);
        ParkItem item = PublicParkHistoryTestData.CreateParkItem("item-1", "Attraction historique", false);
        HistoricalSubject subject = new HistoricalSubject(
            HistoricalSubjectType.ParkItem,
            item.Id,
            item.Name!,
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            park.Id);
        HistoricalFact previous = PublicParkHistoryTestData.CreateOpeningFact(subject, 2001);
        HistoricalSourceReference selectedSource = PublicParkHistoryTestData.CreateSource(previous);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> zones = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IHistoricalFactRepository> facts = new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalRelationRepository> relations =
            new Mock<IHistoricalRelationRepository>(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sources =
            new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        Mock<ISeoSitemapRefreshScheduler> sitemapRefreshScheduler =
            new Mock<ISeoSitemapRefreshScheduler>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdAsync(
                park.Id,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        items.Setup(repository => repository.GetByParkIdAsync(
                park.Id,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { item });
        zones.Setup(repository => repository.GetByParkIdAsync(
                park.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkZone>());
        facts.Setup(repository => repository.GetLatestRevisionAsync(
                previous.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(previous);
        sources.Setup(repository => repository.GetRevisionsAsync(
                It.Is<IReadOnlyCollection<HistoricalSourceRevisionKey>>(revisions =>
                    revisions.Single().SourceId == selectedSource.Id
                    && revisions.Single().Revision == selectedSource.Revision),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { selectedSource });
        facts.Setup(repository => repository.AppendRevisionAsync(
                It.Is<HistoricalFact>(fact =>
                    fact.Id == previous.Id
                    && fact.Revision == previous.Revision + 1
                    && fact.WorkflowState == HistoricalEditorialWorkflowState.Corrected
                    && fact.PublicationState == HistoricalPublicationState.Published),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        sitemapRefreshScheduler
            .Setup(scheduler => scheduler.RequestRefreshAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        HistoricalParkEditorialScopeLoader scopeLoader = new HistoricalParkEditorialScopeLoader(
            parks.Object,
            items.Object,
            zones.Object);
        SaveHistoricalFactCommandHandler handler = new SaveHistoricalFactCommandHandler(
            scopeLoader,
            new HistoricalParkEditorialSubjectResolver(facts.Object, relations.Object),
            facts.Object,
            sources.Object,
            sitemapRefreshScheduler.Object,
            new FixedHistoryEditorialTimeProvider(Now));
        HistoricalFactDraftInput draft = new HistoricalFactDraftInput(
            subject.Type,
            subject.Id,
            previous.Type,
            new HistoricalPeriodInput(
                new HistoricalDateInput(2001, null, null, HistoryDatePrecision.Year, false, null),
                new HistoricalDateInput(2001, null, null, HistoryDatePrecision.Year, false, null),
                PeriodBoundaryConfidence.Confirmed,
                PeriodBoundaryConfidence.Confirmed),
            previous.State,
            previous.Importance,
            previous.PublicUncertaintyExplanation,
            previous.LifecycleBoundaryMeaning,
            previous.AttributeKind,
            previous.AttributeBoundaryMeaning,
            previous.SequenceWithinDate,
            new[]
            {
                new HistoricalEvidenceSourceInput(
                    selectedSource.Id,
                    selectedSource.Revision,
                    HistoricalEvidencePosition.Supports),
            },
            previous.StructuredValue,
            previous.OtherTypeLabel,
            previous.NarrativeContentId,
            park.Id);

        ApplicationResult<HistoricalEditorialMutationResult> result = await handler.HandleAsync(
            new SaveHistoricalFactCommand(
                park.Id,
                previous.Id,
                previous.Revision,
                draft,
                "admin-1",
                "Correction publique"));

        Assert.True(result.IsSuccess);
        parks.VerifyAll();
        items.VerifyAll();
        zones.VerifyAll();
        facts.VerifyAll();
        relations.VerifyNoOtherCalls();
        sources.VerifyAll();
        sitemapRefreshScheduler.VerifyAll();
    }

    private static HistoricalSourceReference CreateSource(int revision)
    {
        return new HistoricalSourceReference(
            Guid.NewGuid(),
            revision,
            HistoricalSourceType.OfficialWebsite,
            "Historique officiel",
            "Parc témoin",
            "https://example.test/history",
            null,
            new DateOnly(2001, 1, 1),
            new DateOnly(2026, 9, 28),
            "fr",
            null,
            new[]
            {
                HistoricalSourceScope.SubjectIdentity,
                HistoricalSourceScope.HistoricalLabel,
                HistoricalSourceScope.FactType,
                HistoricalSourceScope.Period,
            },
            null,
            HistoricalSourceAccessibility.Accessible,
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft,
            Now.UtcDateTime);
    }
}
