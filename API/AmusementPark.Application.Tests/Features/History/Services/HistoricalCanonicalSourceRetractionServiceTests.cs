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
