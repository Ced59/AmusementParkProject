using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.History.Commands;
using AmusementPark.Application.Features.History.Handlers;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Results;
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
}
