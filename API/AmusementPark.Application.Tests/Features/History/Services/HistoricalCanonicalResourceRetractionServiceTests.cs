using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Tests.Features.History.Handlers;
using AmusementPark.Core.Domain.History;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Services;

public sealed class HistoricalCanonicalResourceRetractionServiceTests
{
    [Fact]
    public async Task RetractAsync_ShouldWithdrawFactAndEveryReferencedSource()
    {
        HistoricalSubject subject = new HistoricalSubject(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc témoin",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject);
        HistoricalFact fact = PublicParkHistoryTestData.CreateOpeningFact(subject, 1998);
        HistoricalSourceReference source = PublicParkHistoryTestData.CreateSource(fact);
        Mock<IHistoricalFactRepository> factRepository = new(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository = new(MockBehavior.Strict);
        factRepository
            .Setup(value => value.GetLatestRevisionAsync(fact.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fact);
        factRepository
            .Setup(value => value.AppendRevisionAsync(
                It.Is<HistoricalFact>(candidate => candidate.Id == fact.Id
                    && candidate.PublicationState == HistoricalPublicationState.Withdrawn),
                It.Is<HistoricalReviewEvent>(review => review.EventType == HistoricalReviewEventType.Retracted),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        sourceRepository
            .Setup(value => value.GetLatestRevisionAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        sourceRepository
            .Setup(value => value.AppendRevisionAsync(
                It.Is<HistoricalSourceReference>(candidate => candidate.Id == source.Id
                    && candidate.PublicationState == HistoricalPublicationState.Withdrawn),
                It.Is<HistoricalReviewEvent>(review => review.EventType == HistoricalReviewEventType.Retracted),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        HistoricalCanonicalResourceRetractionService service =
            HistoricalCanonicalResourceRetractionServiceTestFactory.Create(
                factRepository.Object,
                sourceRepository.Object);

        await service.RetractAsync(fact.Id, CancellationToken.None);

        factRepository.VerifyAll();
        sourceRepository.VerifyAll();
    }

    [Fact]
    public async Task RetractAsync_WhenSourceKeepsConflicting_ShouldRestoreRetractedFact()
    {
        HistoricalSubject subject = new HistoricalSubject(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc témoin",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject);
        HistoricalFact fact = PublicParkHistoryTestData.CreateOpeningFact(subject, 1998);
        HistoricalSourceReference source = PublicParkHistoryTestData.CreateSource(fact);
        HistoricalFact factRetraction = fact.CreateRetraction(fact.RecordedAtUtc.AddSeconds(1));
        Mock<IHistoricalFactRepository> factRepository = new(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository = new(MockBehavior.Strict);
        factRepository
            .SetupSequence(value => value.GetLatestRevisionAsync(
                fact.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(fact)
            .ReturnsAsync(fact)
            .ReturnsAsync(factRetraction);
        factRepository
            .Setup(value => value.AppendRevisionAsync(
                It.Is<HistoricalFact>(candidate => candidate.PublicationState
                    == HistoricalPublicationState.Withdrawn),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        factRepository
            .Setup(value => value.AppendRevisionAsync(
                It.Is<HistoricalFact>(candidate => candidate.PublicationState
                    == HistoricalPublicationState.Published),
                It.Is<HistoricalReviewEvent>(review => review.EventType
                    == HistoricalReviewEventType.Published),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        sourceRepository
            .Setup(value => value.GetLatestRevisionAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        sourceRepository
            .Setup(value => value.AppendRevisionAsync(
                It.IsAny<HistoricalSourceReference>(),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Conflict);
        HistoricalCanonicalResourceRetractionService service =
            HistoricalCanonicalResourceRetractionServiceTestFactory.Create(
                factRepository.Object,
                sourceRepository.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RetractAsync(fact.Id, CancellationToken.None));

        factRepository.VerifyAll();
        sourceRepository.VerifyAll();
    }
}
