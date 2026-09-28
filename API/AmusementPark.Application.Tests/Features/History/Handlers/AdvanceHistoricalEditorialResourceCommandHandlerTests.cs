using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Commands;
using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Core.Domain.History;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Handlers;

public sealed class AdvanceHistoricalEditorialResourceCommandHandlerTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 9, 27, 10, 0, 0, DateTimeKind.Utc);
    private static readonly DateTimeOffset Now =
        new DateTimeOffset(2026, 9, 28, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_ForDraftSource_ShouldAppendEditorialReviewRevision()
    {
        HistoricalSourceReference source = CreateDraftSource();
        Mock<IHistoricalFactRepository> facts = new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalRelationRepository> relations =
            new Mock<IHistoricalRelationRepository>(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sources =
            new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        sources.Setup(repository => repository.GetLatestRevisionAsync(
                source.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        sources.Setup(repository => repository.AppendRevisionAsync(
                It.Is<HistoricalSourceReference>(candidate =>
                    candidate.Id == source.Id
                    && candidate.Revision == 2
                    && candidate.WorkflowState == HistoricalEditorialWorkflowState.EditorialReview
                    && candidate.PublicationState == HistoricalPublicationState.Draft
                    && candidate.RecordedAtUtc == Now.UtcDateTime),
                It.Is<HistoricalReviewEvent>(reviewEvent =>
                    reviewEvent.ResourceRevision == 2
                    && reviewEvent.EventType == HistoricalReviewEventType.SubmittedForEditorialReview),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        AdvanceHistoricalEditorialResourceCommandHandler handler = new AdvanceHistoricalEditorialResourceCommandHandler(
            facts.Object,
            relations.Object,
            sources.Object,
            new HistoricalLineagePublicationValidator(relations.Object),
            new FixedHistoryEditorialTimeProvider(Now));

        ApplicationResult<HistoricalEditorialMutationResult> result = await handler.HandleAsync(
            new AdvanceHistoricalEditorialResourceCommand(
                HistoricalReviewResourceType.Source,
                source.Id,
                1,
                "admin-1",
                "Source vérifiée"));

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Revision);
        Assert.Equal(HistoricalEditorialWorkflowState.EditorialReview, result.Value.WorkflowState);
        facts.VerifyNoOtherCalls();
        relations.VerifyNoOtherCalls();
        sources.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WithStaleRevision_ShouldReturnConflictWithoutAppending()
    {
        HistoricalSourceReference source = CreateDraftSource();
        Mock<IHistoricalFactRepository> facts = new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalRelationRepository> relations =
            new Mock<IHistoricalRelationRepository>(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sources =
            new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        sources.Setup(repository => repository.GetLatestRevisionAsync(
                source.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        AdvanceHistoricalEditorialResourceCommandHandler handler = new AdvanceHistoricalEditorialResourceCommandHandler(
            facts.Object,
            relations.Object,
            sources.Object,
            new HistoricalLineagePublicationValidator(relations.Object),
            new FixedHistoryEditorialTimeProvider(Now));

        ApplicationResult<HistoricalEditorialMutationResult> result = await handler.HandleAsync(
            new AdvanceHistoricalEditorialResourceCommand(
                HistoricalReviewResourceType.Source,
                source.Id,
                0,
                "admin-1",
                null));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "history.editorial.revision-conflict");
        facts.VerifyNoOtherCalls();
        relations.VerifyNoOtherCalls();
        sources.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenRelationPublicationWouldCloseLineageCycle_ShouldRejectPublication()
    {
        HistoricalRelation candidate = CreateRelation(
            "item-1",
            "item-2",
            HistoricalEditorialWorkflowState.StructuredValidation,
            HistoricalPublicationState.Draft,
            4,
            3);
        HistoricalRelation existing = CreateRelation(
            "item-2",
            "item-1",
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            4,
            3);
        Mock<IHistoricalFactRepository> facts = new Mock<IHistoricalFactRepository>(MockBehavior.Strict);
        Mock<IHistoricalRelationRepository> relations =
            new Mock<IHistoricalRelationRepository>(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sources =
            new Mock<IHistoricalSourceRepository>(MockBehavior.Strict);
        relations.Setup(repository => repository.GetLatestRevisionAsync(
                candidate.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidate);
        relations.Setup(repository => repository.GetLatestRevisionsForParkAsync(
                "park-1",
                It.Is<IReadOnlyCollection<HistoricalSubjectKey>>(subjects => subjects.Count == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { candidate, existing });
        AdvanceHistoricalEditorialResourceCommandHandler handler = new AdvanceHistoricalEditorialResourceCommandHandler(
            facts.Object,
            relations.Object,
            sources.Object,
            new HistoricalLineagePublicationValidator(relations.Object),
            new FixedHistoryEditorialTimeProvider(Now));

        ApplicationResult<HistoricalEditorialMutationResult> result = await handler.HandleAsync(
            new AdvanceHistoricalEditorialResourceCommand(
                HistoricalReviewResourceType.Relation,
                candidate.Id,
                candidate.Revision,
                "admin-1",
                "Validation structurée"));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "history.editorial.lineage-cycle");
        facts.VerifyNoOtherCalls();
        relations.VerifyAll();
        sources.VerifyNoOtherCalls();
    }

    private static HistoricalSourceReference CreateDraftSource()
    {
        return new HistoricalSourceReference(
            Guid.NewGuid(),
            1,
            HistoricalSourceType.OfficialWebsite,
            "Historique officiel",
            "Parc exemple",
            "https://example.com/history",
            null,
            new DateOnly(2025, 1, 1),
            new DateOnly(2026, 9, 27),
            "fr",
            null,
            new[] { HistoricalSourceScope.Period },
            null,
            HistoricalSourceAccessibility.Accessible,
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft,
            RecordedAtUtc);
    }

    private static HistoricalRelation CreateRelation(
        string sourceId,
        string targetId,
        HistoricalEditorialWorkflowState workflowState,
        HistoricalPublicationState publicationState,
        int revision,
        int? supersedesRevision)
    {
        HistoricalSubject source = new HistoricalSubject(
            HistoricalSubjectType.ParkItem,
            sourceId,
            sourceId,
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");
        HistoricalSubject target = new HistoricalSubject(
            HistoricalSubjectType.ParkItem,
            targetId,
            targetId,
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");
        HistoricalPeriod period = HistoricalPeriod.Point(HistoricalDate.ForYear(2001));
        HistoricalRelationSourceRevisionReference evidence = new HistoricalRelationSourceRevisionReference(
            Guid.NewGuid(),
            1,
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
            });
        bool isPublished = publicationState == HistoricalPublicationState.Published;
        return new HistoricalRelation(
            Guid.NewGuid(),
            source,
            target,
            HistoricalRelationType.ReplacedBy,
            HistoricalRelationDirection.Directed,
            period,
            HistoricalFactState.Verified,
            workflowState,
            publicationState,
            Array.Empty<HistoricalLocalizedText>(),
            new[] { evidence },
            null,
            RecordedAtUtc.AddMinutes(-1),
            isPublished ? RecordedAtUtc : null,
            isPublished ? "historical-editor-v1" : null,
            revision,
            supersedesRevision,
            RecordedAtUtc);
    }
}
