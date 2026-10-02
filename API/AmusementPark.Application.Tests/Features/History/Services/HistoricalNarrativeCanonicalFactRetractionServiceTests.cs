using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Tests.Features.History.Handlers;
using AmusementPark.Core.Domain.History;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Services;

public sealed class HistoricalNarrativeCanonicalFactRetractionServiceTests
{
    [Fact]
    public async Task RetractAsync_ShouldReturnAndObserveTheExactRetractedRevision()
    {
        HistoricalSubject subject = new HistoricalSubject(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc témoin",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject);
        HistoricalFact fact = PublicParkHistoryTestData.CreateOpeningFact(subject, 1998);
        Mock<IHistoricalFactRepository> repository = new(MockBehavior.Strict);
        repository
            .Setup(value => value.GetLatestRevisionAsync(fact.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fact);
        repository
            .Setup(value => value.AppendRevisionAsync(
                It.Is<HistoricalFact>(candidate => candidate.SupersedesRevision == fact.Revision),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        HistoricalNarrativeCanonicalFactRetractionService service = new(repository.Object);
        HistoricalFact? observed = null;

        HistoricalFact result = await service.RetractAsync(
            fact.Id,
            snapshot => observed = snapshot,
            CancellationToken.None);

        Assert.Same(fact, observed);
        Assert.Same(fact, result);
        repository.VerifyAll();
    }

    [Fact]
    public async Task RestoreAsync_WhenSnapshotWasInStructuredValidation_ShouldUseValidationEvent()
    {
        HistoricalSubject subject = new HistoricalSubject(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc témoin",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject);
        HistoricalFact published = PublicParkHistoryTestData.CreateOpeningFact(subject, 1998);
        HistoricalFact snapshot = CreateReviewRevision(published);
        HistoricalFact retraction = snapshot.CreateRetraction(snapshot.RecordedAtUtc.AddSeconds(1));
        Mock<IHistoricalFactRepository> repository = new(MockBehavior.Strict);
        repository
            .Setup(value => value.GetLatestRevisionAsync(snapshot.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(retraction);
        repository
            .Setup(value => value.AppendRevisionAsync(
                It.Is<HistoricalFact>(candidate => candidate.WorkflowState
                    == HistoricalEditorialWorkflowState.StructuredValidation),
                It.Is<HistoricalReviewEvent>(review => review.EventType
                    == HistoricalReviewEventType.StructuredValidationCompleted),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        HistoricalNarrativeCanonicalFactRetractionService service = new(repository.Object);

        await service.RestoreAsync(snapshot, CancellationToken.None);

        repository.VerifyAll();
    }

    private static HistoricalFact CreateReviewRevision(HistoricalFact published)
    {
        return new HistoricalFact(
            published.Id,
            published.Subject,
            published.Type,
            published.Period,
            HistoricalFactState.Unverified,
            published.Importance,
            HistoricalEditorialWorkflowState.StructuredValidation,
            HistoricalPublicationState.Draft,
            Array.Empty<HistoricalLocalizedText>(),
            published.LifecycleBoundaryMeaning,
            published.AttributeKind,
            published.AttributeBoundaryMeaning,
            published.SequenceWithinDate,
            published.SourceReferences,
            published.StructuredValue,
            published.OtherTypeLabel,
            published.NarrativeContentId,
            null,
            null,
            null,
            published.Revision + 1,
            published.Revision,
            published.RecordedAtUtc.AddSeconds(1),
            published.RevisionOrigin);
    }
}
