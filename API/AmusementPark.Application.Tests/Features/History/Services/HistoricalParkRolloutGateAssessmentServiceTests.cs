using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Tests.Features.History.Handlers;
using AmusementPark.Core.Domain.History;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Services;

public sealed class HistoricalParkRolloutGateAssessmentServiceTests
{
    [Fact]
    public async Task AssessAsync_ShouldReuseKeyYearEligibilityWithoutEnumeratingArbitraryYears()
    {
        HistoricalSubject parkSubject = new(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc témoin",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");
        HistoricalSubject itemSubject = new(
            HistoricalSubjectType.ParkItem,
            "item-1",
            "Attraction témoin",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");
        HistoricalFact majorFact = PublicParkHistoryTestData.CreateOpeningFact(parkSubject, 1998);
        HistoricalFact supportingFact = PublicParkHistoryTestData.CreateOpeningFact(
            itemSubject,
            1990,
            importance: HistoricalImportance.Standard);
        Mock<IParkHistoricalSnapshotBuilder> snapshotBuilder = new(MockBehavior.Strict);
        snapshotBuilder.Setup(builder => builder.Build(
                "park-1",
                It.Is<HistoricalInstant>(instant => instant.Year == 1998),
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<IReadOnlyCollection<HistoricalFact>>()))
            .Returns(CreateEligibleSnapshot(parkSubject, majorFact.Id, supportingFact.Id));
        HistoricalParkRolloutGateAssessmentService service = new(
            snapshotBuilder.Object,
            new HistoricalParkRolloutGateEvaluator(),
            PublicParkHistoryTestData.CreatePublicSourceRepository(
                new[] { majorFact, supportingFact }));

        HistoricalParkRolloutGate result = await service.AssessAsync(
            "park-1",
            new[] { parkSubject, itemSubject },
            new[] { majorFact, supportingFact },
            CancellationToken.None);

        Assert.True(result.IsOpen);
        Assert.Equal(new[] { 1998 }, result.IndexableKeyYears);
        snapshotBuilder.VerifyAll();
        snapshotBuilder.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AssessAsync_WhenLatestSourceRevisionIsWithdrawn_ShouldCloseGate()
    {
        HistoricalSubject parkSubject = new(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc témoin",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");
        HistoricalFact majorFact = PublicParkHistoryTestData.CreateOpeningFact(parkSubject, 1998);
        HistoricalFact supportingFact = PublicParkHistoryTestData.CreateOpeningFact(
            parkSubject,
            1990,
            importance: HistoricalImportance.Standard);
        HistoricalSourceReference majorSource = PublicParkHistoryTestData.CreateSource(majorFact);
        HistoricalSourceReference supportingSource = PublicParkHistoryTestData.CreateSource(supportingFact);
        HistoricalSourceReference withdrawnSupportingSource = CreateWithdrawnRevision(supportingSource);
        Mock<IHistoricalSourceRepository> sources = new(MockBehavior.Strict);
        sources
            .Setup(repository => repository.GetRevisionsAsync(
                It.IsAny<IReadOnlyCollection<HistoricalSourceRevisionReference>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { majorSource, supportingSource });
        sources
            .Setup(repository => repository.GetLatestRevisionsAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { majorSource, withdrawnSupportingSource });
        Mock<IParkHistoricalSnapshotBuilder> snapshotBuilder = new(MockBehavior.Strict);
        snapshotBuilder
            .Setup(builder => builder.Build(
                "park-1",
                It.IsAny<HistoricalInstant>(),
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<IReadOnlyCollection<HistoricalFact>>()))
            .Returns(CreateEligibleSnapshot(parkSubject, majorFact.Id, supportingFact.Id));
        HistoricalParkRolloutGateAssessmentService service = new(
            snapshotBuilder.Object,
            new HistoricalParkRolloutGateEvaluator(),
            sources.Object);

        HistoricalParkRolloutGate result = await service.AssessAsync(
            "park-1",
            new[] { parkSubject },
            new[] { majorFact, supportingFact },
            CancellationToken.None);

        Assert.False(result.IsOpen);
        Assert.False(result.HasCompleteSourceCoverage);
        Assert.Equal(1, result.SourcedFactCount);
        sources.VerifyAll();
    }

    [Fact]
    public async Task AssessManyAsync_ShouldLoadSourceRevisionsOnceForAllParks()
    {
        HistoricalSubject firstPark = new(
            HistoricalSubjectType.Park,
            "park-1",
            "Premier parc",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");
        HistoricalSubject secondPark = new(
            HistoricalSubjectType.Park,
            "park-2",
            "Second parc",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-2");
        HistoricalFact[] firstFacts =
        {
            PublicParkHistoryTestData.CreateOpeningFact(firstPark, 1998),
            PublicParkHistoryTestData.CreateOpeningFact(
                firstPark,
                1990,
                importance: HistoricalImportance.Standard),
        };
        HistoricalFact[] secondFacts =
        {
            PublicParkHistoryTestData.CreateOpeningFact(secondPark, 2001),
            PublicParkHistoryTestData.CreateOpeningFact(
                secondPark,
                1995,
                importance: HistoricalImportance.Standard),
        };
        HistoricalSourceReference[] sourceRevisions = firstFacts
            .Concat(secondFacts)
            .Select(PublicParkHistoryTestData.CreateSource)
            .ToArray();
        Mock<IHistoricalSourceRepository> sources = new(MockBehavior.Strict);
        sources
            .Setup(repository => repository.GetRevisionsAsync(
                It.Is<IReadOnlyCollection<HistoricalSourceRevisionReference>>(references =>
                    references.Count == 4),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceRevisions);
        sources
            .Setup(repository => repository.GetLatestRevisionsAsync(
                It.Is<IReadOnlyCollection<Guid>>(sourceIds => sourceIds.Count == 4),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(sourceRevisions);
        Mock<IParkHistoricalSnapshotBuilder> snapshotBuilder = new(MockBehavior.Strict);
        snapshotBuilder
            .Setup(builder => builder.Build(
                It.IsAny<string>(),
                It.IsAny<HistoricalInstant>(),
                It.IsAny<IReadOnlyCollection<HistoricalSubject>>(),
                It.IsAny<IReadOnlyCollection<HistoricalFact>>()))
            .Returns((
                string parkId,
                HistoricalInstant instant,
                IReadOnlyCollection<HistoricalSubject> subjects,
                IReadOnlyCollection<HistoricalFact> facts) => CreateEligibleSnapshot(
                    parkId,
                    instant,
                    subjects.Single(),
                    facts.Select(static fact => fact.Id).ToArray()));
        HistoricalParkRolloutGateAssessmentService service = new(
            snapshotBuilder.Object,
            new HistoricalParkRolloutGateEvaluator(),
            sources.Object);

        IReadOnlyDictionary<string, HistoricalParkRolloutGate> result =
            await service.AssessManyAsync(
                new[]
                {
                    new HistoricalParkRolloutGateAssessmentRequest(
                        firstPark.ContextParkId!,
                        new[] { firstPark },
                        firstFacts),
                    new HistoricalParkRolloutGateAssessmentRequest(
                        secondPark.ContextParkId!,
                        new[] { secondPark },
                        secondFacts),
                },
                CancellationToken.None);

        Assert.True(result["park-1"].IsOpen);
        Assert.True(result["park-2"].IsOpen);
        sources.Verify(repository => repository.GetRevisionsAsync(
            It.IsAny<IReadOnlyCollection<HistoricalSourceRevisionReference>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        sources.Verify(repository => repository.GetLatestRevisionsAsync(
            It.IsAny<IReadOnlyCollection<Guid>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        sources.VerifyNoOtherCalls();
    }

    private static ParkHistoricalSnapshot CreateEligibleSnapshot(
        HistoricalSubject subject,
        params Guid[] supportingFactIds)
    {
        return CreateEligibleSnapshot(
            "park-1",
            HistoricalInstant.ForYear(1998),
            subject,
            supportingFactIds);
    }

    private static ParkHistoricalSnapshot CreateEligibleSnapshot(
        string parkId,
        HistoricalInstant instant,
        HistoricalSubject subject,
        params Guid[] supportingFactIds)
    {
        HistoricalSubjectSnapshot subjectSnapshot = new(
            subject,
            HistoricalOperationalState.Unknown,
            HistoricalPresenceExtent.None,
            Array.Empty<HistoricalPresenceInterval>(),
            Array.Empty<HistoricalAttributeSnapshot>(),
            Array.Empty<HistoricalSnapshotReason>(),
            supportingFactIds);
        HistoricalCoverage coverage = new(
            1,
            1,
            0,
            0,
            new HistoricalFieldCoverage(1, 1),
            new HistoricalFieldCoverage(0, 0),
            DateTime.UtcNow,
            HistoricalCoverageStatus.Substantial);
        return new ParkHistoricalSnapshot(
            parkId,
            instant,
            new[] { subjectSnapshot },
            coverage,
            Array.Empty<HistoricalAmbiguity>(),
            ParkHistoricalSnapshotBuilder.CurrentMethodologyVersion);
    }

    private static HistoricalSourceReference CreateWithdrawnRevision(
        HistoricalSourceReference source)
    {
        return new HistoricalSourceReference(
            source.Id,
            source.Revision + 1,
            source.Type,
            source.Title,
            source.PublisherOrAuthor,
            source.Url,
            source.BibliographicReference,
            source.PublishedOn,
            source.AccessedOn,
            source.LanguageCode,
            source.ArchiveUrl,
            source.Scopes,
            source.AdminNote,
            HistoricalSourceAccessibility.Withdrawn,
            HistoricalEditorialWorkflowState.Retracted,
            HistoricalPublicationState.Withdrawn,
            source.RecordedAtUtc.AddMinutes(1),
            source.RevisionOrigin);
    }
}
