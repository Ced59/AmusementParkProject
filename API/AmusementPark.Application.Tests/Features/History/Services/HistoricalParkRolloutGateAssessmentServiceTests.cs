using AmusementPark.Application.Features.History.Services;
using AmusementPark.Application.Tests.Features.History.Handlers;
using AmusementPark.Core.Domain.History;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Services;

public sealed class HistoricalParkRolloutGateAssessmentServiceTests
{
    [Fact]
    public void Assess_ShouldReuseKeyYearEligibilityWithoutEnumeratingArbitraryYears()
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
            new HistoricalParkRolloutGateEvaluator());

        HistoricalParkRolloutGate result = service.Assess(
            "park-1",
            new[] { parkSubject, itemSubject },
            new[] { majorFact, supportingFact });

        Assert.True(result.IsOpen);
        Assert.Equal(new[] { 1998 }, result.IndexableKeyYears);
        snapshotBuilder.VerifyAll();
        snapshotBuilder.VerifyNoOtherCalls();
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
}
