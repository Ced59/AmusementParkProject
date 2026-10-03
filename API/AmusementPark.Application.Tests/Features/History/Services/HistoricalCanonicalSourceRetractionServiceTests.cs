using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Tests.Features.History.Handlers;
using AmusementPark.Core.Domain.History;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Services;

public sealed class HistoricalCanonicalSourceRetractionServiceTests
{
    [Fact]
    public async Task RetractAsync_WhenAppendConflicts_ShouldClearCompensationBeforeRetryRead()
    {
        HistoricalSubject subject = new HistoricalSubject(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc témoin",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject);
        HistoricalFact fact = PublicParkHistoryTestData.CreateOpeningFact(subject, 1998);
        HistoricalSourceReference source = PublicParkHistoryTestData.CreateSource(fact);
        Mock<IHistoricalSourceRepository> repository = new(MockBehavior.Strict);
        repository
            .SetupSequence(value => value.GetLatestRevisionAsync(
                source.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(source)
            .ThrowsAsync(new InvalidOperationException("read failed"));
        repository
            .Setup(value => value.AppendRevisionAsync(
                It.IsAny<HistoricalSourceReference>(),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Conflict);
        HistoricalCanonicalSourceRetractionService service = new(repository.Object);
        HistoricalSourceReference? compensationSnapshot = null;

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RetractAsync(
            source.Id,
            snapshot => compensationSnapshot = snapshot,
            CancellationToken.None));

        Assert.Null(compensationSnapshot);
        repository.VerifyAll();
    }

    [Fact]
    public async Task RestoreAsync_WhenSnapshotWasInEditorialReview_ShouldUseSubmissionEvent()
    {
        HistoricalSubject subject = new HistoricalSubject(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc témoin",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject);
        HistoricalFact fact = PublicParkHistoryTestData.CreateOpeningFact(subject, 1998);
        HistoricalSourceReference published = PublicParkHistoryTestData.CreateSource(fact);
        HistoricalSourceReference snapshot = CreateRevision(
            published,
            HistoricalEditorialWorkflowState.EditorialReview,
            HistoricalPublicationState.Draft,
            HistoricalSourceAccessibility.Accessible);
        HistoricalSourceReference retraction = CreateRevision(
            snapshot,
            HistoricalEditorialWorkflowState.Retracted,
            HistoricalPublicationState.Withdrawn,
            HistoricalSourceAccessibility.Withdrawn);
        Mock<IHistoricalSourceRepository> repository = new(MockBehavior.Strict);
        repository
            .Setup(value => value.GetLatestRevisionAsync(snapshot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(retraction);
        repository
            .Setup(value => value.AppendRevisionAsync(
                It.Is<HistoricalSourceReference>(candidate => candidate.WorkflowState
                    == HistoricalEditorialWorkflowState.EditorialReview),
                It.Is<HistoricalReviewEvent>(review => review.EventType
                    == HistoricalReviewEventType.SubmittedForEditorialReview),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        HistoricalCanonicalSourceRetractionService service = new(repository.Object);

        await service.RestoreAsync(snapshot, CancellationToken.None);

        repository.VerifyAll();
    }

    private static HistoricalSourceReference CreateRevision(
        HistoricalSourceReference previous,
        HistoricalEditorialWorkflowState workflowState,
        HistoricalPublicationState publicationState,
        HistoricalSourceAccessibility accessibility)
    {
        return new HistoricalSourceReference(
            previous.Id,
            previous.Revision + 1,
            previous.Type,
            previous.Title,
            previous.PublisherOrAuthor,
            previous.Url,
            previous.BibliographicReference,
            previous.PublishedOn,
            previous.AccessedOn,
            previous.LanguageCode,
            previous.ArchiveUrl,
            previous.Scopes,
            previous.AdminNote,
            accessibility,
            workflowState,
            publicationState,
            previous.RecordedAtUtc.AddSeconds(1),
            previous.RevisionOrigin);
    }
}
