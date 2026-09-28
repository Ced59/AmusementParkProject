using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Features.History.Ports;
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

    private static ParkHistoricalSnapshot CreateEligibleSnapshot(
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
            "park-1",
            HistoricalInstant.ForYear(1998),
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
