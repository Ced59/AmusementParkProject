using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Tests.Features.History.Handlers;
using AmusementPark.Core.Domain.History;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Services;

public sealed class HistoricalNarrativeCanonicalLinkerTests
{
    [Fact]
    public async Task LinkAsync_WhenPersistenceKeepsThrowing_ShouldWithdrawUnlinkedResources()
    {
        HistoricalSubject subject = new HistoricalSubject(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc témoin",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject);
        HistoricalFact fact = PublicParkHistoryTestData.CreateOpeningFact(subject, 1998);
        HistoricalSourceReference source = PublicParkHistoryTestData.CreateSource(fact);
        HistoryEvent narrative = new HistoryEvent
        {
            Id = "history-1",
            UpdatedAtUtc = new DateTime(2026, 10, 3, 9, 0, 0, DateTimeKind.Utc),
        };
        HistoricalNarrativeCanonicalizationResult canonicalization = new(
            fact.Id,
            HistoricalNarrativeCanonicalizationState.Canonicalized,
            Array.Empty<string>());
        Mock<IHistoryEventRepository> historyRepository = new(MockBehavior.Strict);
        Mock<IHistoricalFactRepository> factRepository = new(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository = new(MockBehavior.Strict);
        List<string> operations = new List<string>();
        historyRepository
            .SetupSequence(value => value.SetCanonicalizationAsync(
                narrative.Id,
                narrative.UpdatedAtUtc,
                fact.Id,
                HistoricalNarrativeCanonicalizationState.Canonicalized,
                HistoricalNarrativeCanonicalizationService.CanonicalizationVersion,
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("first acknowledgement lost"))
            .ThrowsAsync(new TimeoutException("retry failed"));
        historyRepository
            .Setup(value => value.GetByIdAsync(
                narrative.Id,
                true,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((HistoryEvent?)null);
        factRepository
            .Setup(value => value.GetLatestRevisionAsync(fact.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fact);
        factRepository
            .Setup(value => value.AppendRevisionAsync(
                It.Is<HistoricalFact>(candidate => candidate.Id == fact.Id
                    && candidate.PublicationState == HistoricalPublicationState.Withdrawn),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => operations.Add("fact-cleaned"))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        sourceRepository
            .Setup(value => value.GetLatestRevisionAsync(source.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(source);
        sourceRepository
            .Setup(value => value.AppendRevisionAsync(
                It.Is<HistoricalSourceReference>(candidate => candidate.Id == source.Id
                    && candidate.PublicationState == HistoricalPublicationState.Withdrawn),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => operations.Add("source-cleaned"))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);

        await Assert.ThrowsAsync<AggregateException>(() =>
            HistoricalNarrativeCanonicalLinker.LinkAsync(
                historyRepository.Object,
                HistoricalCanonicalResourceRetractionServiceTestFactory.Create(
                    factRepository.Object,
                    sourceRepository.Object),
                narrative,
                canonicalization,
                CancellationToken.None,
                _ =>
                {
                    operations.Add("previous-restored");
                    return Task.CompletedTask;
                }));

        Assert.Equal(
            new[] { "fact-cleaned", "source-cleaned", "previous-restored" },
            operations);
        historyRepository.VerifyAll();
        factRepository.VerifyAll();
        sourceRepository.VerifyAll();
    }
}
