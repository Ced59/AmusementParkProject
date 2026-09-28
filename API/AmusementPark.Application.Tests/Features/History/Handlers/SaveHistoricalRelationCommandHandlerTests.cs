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
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Handlers;

public sealed class SaveHistoricalRelationCommandHandlerTests
{
    private static readonly DateTimeOffset Now =
        new DateTimeOffset(2026, 9, 28, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_WhenPublishedCorrectionWouldCloseLineageCycle_ShouldRejectCorrection()
    {
        HistoricalSourceReference source = CreateSource();
        HistoricalSubject first = CreateSubject("item-1");
        HistoricalSubject second = CreateSubject("item-2");
        HistoricalRelation previous = CreatePublishedRelation(
            first,
            second,
            HistoricalRelationType.SamePhysicalAssetAs,
            HistoricalRelationDirection.Symmetric,
            source.Id);
        HistoricalRelation reverse = CreatePublishedRelation(
            second,
            first,
            HistoricalRelationType.ReplacedBy,
            HistoricalRelationDirection.Directed,
            source.Id);
        Park park = PublicParkHistoryTestData.CreatePark(false);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Mock<IParkZoneRepository> zones = new Mock<IParkZoneRepository>(MockBehavior.Strict);
        Mock<IHistoricalFactRepository> facts = new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalRelationRepository> relations =
            new Mock<IHistoricalRelationRepository>(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sources =
            new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        parks.Setup(repository => repository.GetByIdAsync(
                park.Id,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        items.Setup(repository => repository.GetByParkIdAsync(
                park.Id,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                PublicParkHistoryTestData.CreateParkItem(first.Id, first.HistoricalLabel, false),
                PublicParkHistoryTestData.CreateParkItem(second.Id, second.HistoricalLabel, false),
            });
        zones.Setup(repository => repository.GetByParkIdAsync(
                park.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ParkZone>());
        relations.Setup(repository => repository.GetLatestRevisionAsync(
                previous.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(previous);
        relations.Setup(repository => repository.GetLatestDecisionEligibleRevisionsTouchingSubjectsAsync(
                It.Is<IReadOnlyCollection<HistoricalSubjectKey>>(subjects =>
                    subjects.Count == 1
                    && subjects.Single().Id == second.Id),
                200,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { previous, reverse });
        sources.Setup(repository => repository.GetRevisionsAsync(
                It.Is<IReadOnlyCollection<HistoricalSourceRevisionKey>>(revisions =>
                    revisions.Count == 1
                    && revisions.Single().SourceId == source.Id
                    && revisions.Single().Revision == source.Revision),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { source });
        HistoricalParkEditorialScopeLoader scopeLoader = new HistoricalParkEditorialScopeLoader(
            parks.Object,
            items.Object,
            zones.Object);
        SaveHistoricalRelationCommandHandler handler = new SaveHistoricalRelationCommandHandler(
            scopeLoader,
            new HistoricalParkEditorialSubjectResolver(facts.Object, relations.Object),
            new HistoricalLineagePublicationValidator(relations.Object),
            relations.Object,
            sources.Object,
            new FixedHistoryEditorialTimeProvider(Now));
        HistoricalPeriodInput period = new HistoricalPeriodInput(
            new HistoricalDateInput(2001, null, null, HistoryDatePrecision.Year, false, null),
            new HistoricalDateInput(2001, null, null, HistoryDatePrecision.Year, false, null),
            PeriodBoundaryConfidence.Confirmed,
            PeriodBoundaryConfidence.Confirmed);
        HistoricalRelationDraftInput draft = new HistoricalRelationDraftInput(
            first.Type,
            first.Id,
            second.Type,
            second.Id,
            HistoricalRelationType.ReplacedBy,
            HistoricalRelationDirection.Directed,
            period,
            HistoricalFactState.Verified,
            Array.Empty<HistoricalLocalizedText>(),
            new[]
            {
                new HistoricalEvidenceSourceInput(
                    source.Id,
                    source.Revision,
                    HistoricalEvidencePosition.Supports),
            },
            "Correction de la relation.",
            first.ContextParkId,
            second.ContextParkId);

        ApplicationResult<HistoricalEditorialMutationResult> result = await handler.HandleAsync(
            new SaveHistoricalRelationCommand(
                park.Id,
                previous.Id,
                previous.Revision,
                draft,
                "admin-1",
                "Correction vérifiée"));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "history.editorial.lineage-cycle");
        parks.VerifyAll();
        items.VerifyAll();
        zones.VerifyAll();
        facts.VerifyNoOtherCalls();
        relations.VerifyAll();
        sources.VerifyAll();
    }

    private static HistoricalSubject CreateSubject(string id)
    {
        return new HistoricalSubject(
            HistoricalSubjectType.ParkItem,
            id,
            id,
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");
    }

    private static HistoricalRelation CreatePublishedRelation(
        HistoricalSubject source,
        HistoricalSubject target,
        HistoricalRelationType type,
        HistoricalRelationDirection direction,
        Guid sourceId)
    {
        HistoricalPeriod period = HistoricalPeriod.Point(HistoricalDate.ForYear(2001));
        HistoricalRelationSourceRevisionReference evidence = new HistoricalRelationSourceRevisionReference(
            sourceId,
            2,
            new HistoricalSubjectKey(source.Type, source.Id, source.ContextParkId),
            new HistoricalSubjectKey(target.Type, target.Id, target.ContextParkId),
            type,
            period,
            HistoricalEvidencePosition.Supports,
            new[]
            {
                HistoricalSourceScope.RelationSourceIdentity,
                HistoricalSourceScope.RelationTargetIdentity,
                HistoricalSourceScope.RelationType,
                HistoricalSourceScope.Period,
            });
        return new HistoricalRelation(
            Guid.NewGuid(),
            source,
            target,
            type,
            direction,
            period,
            HistoricalFactState.Verified,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            Array.Empty<HistoricalLocalizedText>(),
            new[] { evidence },
            null,
            Now.UtcDateTime.AddMinutes(-2),
            Now.UtcDateTime.AddMinutes(-1),
            "historical-editor-v1",
            4,
            3,
            Now.UtcDateTime);
    }

    private static HistoricalSourceReference CreateSource()
    {
        return new HistoricalSourceReference(
            Guid.NewGuid(),
            2,
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
                HistoricalSourceScope.RelationSourceIdentity,
                HistoricalSourceScope.RelationTargetIdentity,
                HistoricalSourceScope.RelationType,
                HistoricalSourceScope.Period,
            },
            null,
            HistoricalSourceAccessibility.Accessible,
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft,
            Now.UtcDateTime);
    }
}
