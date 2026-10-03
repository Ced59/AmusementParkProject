using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Services;

public sealed class HistoricalNarrativeCanonicalizationMigrationTests
{
    [Fact]
    public async Task ExecuteAsync_WhenReplacingFact_RetractsPreviousResourceBeforeFinalLink()
    {
        Guid previousFactId = Guid.NewGuid();
        DateTime recordedAtUtc = new DateTime(2026, 10, 3, 10, 0, 0, DateTimeKind.Utc);
        HistoryEvent narrative = new HistoryEvent
        {
            Id = "history-1",
            Key = "park-opening",
            EntityType = HistoryEntityType.Park,
            OwnerId = "park-1",
            ParkId = "park-1",
            Year = 1998,
            DatePrecision = HistoryDatePrecision.Year,
            EventType = ParkHistoryEventType.Opening.ToString(),
            IsVisible = false,
            CanonicalFactId = previousFactId,
            CanonicalizationState = HistoricalNarrativeCanonicalizationState.PendingReview,
            CreatedAtUtc = recordedAtUtc,
            UpdatedAtUtc = recordedAtUtc,
        };
        HistoricalFact previousFact = CreatePreviousFact(previousFactId, recordedAtUtc.AddMinutes(-1));
        List<string> operations = new List<string>();
        Mock<IHistoryEventRepository> historyRepository = new(MockBehavior.Strict);
        Mock<IHistoricalFactRepository> factRepository = new(MockBehavior.Strict);
        Mock<IHistoricalSourceRepository> sourceRepository = new(MockBehavior.Strict);
        Mock<IParkRepository> parkRepository = new(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new(MockBehavior.Strict);
        historyRepository
            .Setup(value => value.GetCanonicalizationCandidatesAsync(
                HistoricalNarrativeCanonicalizationService.CanonicalizationVersion,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { narrative });
        parkRepository
            .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park
            {
                Id = "park-1",
                Name = "Parc exemple",
                IsVisible = true,
                AdminReviewStatus = AdminReviewStatus.Validated,
            });
        factRepository
            .Setup(value => value.AppendRevisionAsync(
                It.Is<HistoricalFact>(fact => fact.Id != previousFactId),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => operations.Add("canonical"))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        factRepository
            .Setup(value => value.GetLatestRevisionAsync(
                previousFactId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousFact);
        factRepository
            .Setup(value => value.AppendRevisionAsync(
                It.Is<HistoricalFact>(fact => fact.Id == previousFactId
                    && fact.PublicationState == HistoricalPublicationState.Withdrawn),
                It.IsAny<HistoricalReviewEvent>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => operations.Add("retract-old"))
            .ReturnsAsync(HistoricalRevisionWriteDisposition.Created);
        historyRepository
            .Setup(value => value.SetCanonicalizationAsync(
                narrative.Id,
                narrative.UpdatedAtUtc,
                It.IsAny<Guid?>(),
                HistoricalNarrativeCanonicalizationState.Canonicalized,
                HistoricalNarrativeCanonicalizationService.CanonicalizationVersion,
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .Callback(() => operations.Add("link"))
            .ReturnsAsync(true);
        HistoricalNarrativeCanonicalSourcePlanner sourcePlanner =
            new HistoricalNarrativeCanonicalSourcePlanner();
        HistoricalNarrativeCanonicalizationService canonicalizationService = new(
            new HistoricalNarrativeCanonicalSubjectResolver(
                parkRepository.Object,
                parkItemRepository.Object),
            sourcePlanner,
            new HistoricalNarrativeCanonicalFactFactory(),
            new HistoricalNarrativeCanonicalRevisionWriter(
                factRepository.Object,
                sourceRepository.Object,
                sourcePlanner));
        HistoricalCanonicalResourceRetractionService retractionService =
            HistoricalCanonicalResourceRetractionServiceTestFactory.Create(
                factRepository.Object,
                sourceRepository.Object);
        HistoricalNarrativeCanonicalizationMigration migration = new(
            historyRepository.Object,
            canonicalizationService,
            retractionService);

        await migration.ExecuteAsync(CancellationToken.None);

        Assert.Equal(new[] { "canonical", "retract-old", "link" }, operations);
        historyRepository.VerifyAll();
        factRepository.VerifyAll();
        sourceRepository.VerifyNoOtherCalls();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
    }

    private static HistoricalFact CreatePreviousFact(Guid factId, DateTime recordedAtUtc)
    {
        return new HistoricalFact(
            factId,
            new HistoricalSubject(
                HistoricalSubjectType.Park,
                "park-1",
                "Parc exemple",
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject),
            HistoricalFactType.Opening,
            HistoricalPeriod.Point(HistoricalDate.ForYear(1998)),
            HistoricalFactState.Unverified,
            HistoricalImportance.Standard,
            HistoricalEditorialWorkflowState.EditorialReview,
            HistoricalPublicationState.LegacyPublishedPendingReview,
            HistoricalLocalizationPolicy.SupportedLanguageCodes
                .Select(static code => new HistoricalLocalizedText(code, "À vérifier."))
                .ToArray(),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null,
            null,
            Array.Empty<HistoricalSourceRevisionReference>(),
            null,
            null,
            "history-1",
            null,
            null,
            "hist-v1-legacy",
            1,
            null,
            recordedAtUtc,
            HistoricalRevisionOrigin.LegacyMigration);
    }
}
