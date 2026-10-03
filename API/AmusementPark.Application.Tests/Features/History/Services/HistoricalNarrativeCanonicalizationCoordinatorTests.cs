using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Tests.Features.History.Handlers;
using AmusementPark.Core.Domain.History;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Services;

public sealed class HistoricalNarrativeCanonicalizationCoordinatorTests
{
    [Fact]
    public async Task ExecuteAsync_WhenCandidateCleanupFails_ShouldNotRestorePreviousResources()
    {
        DateTime updatedAtUtc = new DateTime(2026, 10, 3, 10, 0, 0, DateTimeKind.Utc);
        HistoryEvent narrative = new HistoryEvent
        {
            Id = "history-1",
            UpdatedAtUtc = updatedAtUtc,
            Sources = new List<HistorySourceReference>(),
        };
        Guid generatedFactId = HistoricalNarrativeCanonicalIdentity.CreateGuid(
            HistoricalNarrativeCanonicalizationService.CanonicalizationVersion,
            "fact",
            narrative.Id,
            updatedAtUtc);
        Guid previousFactId = Guid.NewGuid();
        HistoricalSubject subject = new HistoricalSubject(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc témoin",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject);
        HistoricalFact generatedFact = PublicParkHistoryTestData.CreateOpeningFact(
            subject,
            1999,
            generatedFactId);
        HistoricalFact previousFact = PublicParkHistoryTestData.CreateOpeningFact(
            subject,
            1998,
            previousFactId);
        HistoricalCanonicalResourceRetractionSnapshot previousSnapshot = new(
            previousFact,
            Array.Empty<HistoricalSourceReference>());
        Mock<IHistoryEventRepository> historyRepository = new(MockBehavior.Strict);
        Mock<IHistoricalFactRepository> factRepository = new(MockBehavior.Strict);
        Mock<IHistoricalNarrativeCanonicalizer> canonicalizer = new(MockBehavior.Strict);
        canonicalizer
            .Setup(value => value.CanonicalizeAsync(
                narrative,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("canonicalization failed"));
        factRepository
            .Setup(value => value.GetLatestRevisionAsync(
                generatedFactId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(generatedFact);
        factRepository
            .Setup(value => value.AppendRevisionAsync(
                It.Is<HistoricalFact>(fact => fact.Id == generatedFactId
                    && fact.PublicationState == HistoricalPublicationState.Withdrawn),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("candidate cleanup failed"));
        HistoricalCanonicalResourceRetractionService resourceRetractionService =
            HistoricalCanonicalResourceRetractionServiceTestFactory.Create(
                factRepository.Object);

        AggregateException exception = await Assert.ThrowsAsync<AggregateException>(() =>
            HistoricalNarrativeCanonicalizationCoordinator.ExecuteAsync(
                historyRepository.Object,
                canonicalizer.Object,
                resourceRetractionService,
                narrative,
                previousFactId,
                previousSnapshot,
                CancellationToken.None));

        Assert.Contains("could not withdraw", exception.Message, StringComparison.Ordinal);
        Assert.Contains(
            exception.InnerExceptions,
            inner => inner is TimeoutException
                && inner.Message == "canonicalization failed");
        Assert.Contains(
            exception.InnerExceptions,
            inner => inner is AggregateException
                && inner.Message.Contains("could not be fully withdrawn", StringComparison.Ordinal));
        historyRepository.VerifyNoOtherCalls();
        factRepository.VerifyAll();
        canonicalizer.VerifyAll();
    }
}
