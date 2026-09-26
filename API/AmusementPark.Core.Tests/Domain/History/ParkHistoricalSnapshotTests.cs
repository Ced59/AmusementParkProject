using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class ParkHistoricalSnapshotTests
{
    private static readonly HistoricalSubject Subject = new HistoricalSubject(
        HistoricalSubjectType.ParkItem,
        "item-1",
        "Élément historique",
        HistoricalSubjectPublicationPolicy.FollowCurrentSubject);

    [Fact]
    public void Constructor_WithCoverageForAnotherPopulation_Throws()
    {
        HistoricalSubjectSnapshot subjectSnapshot = CreateSubjectSnapshot(Subject);
        HistoricalCoverage emptyCoverage = new HistoricalCoverage(
            0,
            0,
            0,
            0,
            new HistoricalFieldCoverage(0, 0),
            new HistoricalFieldCoverage(0, 0),
            null,
            HistoricalCoverageStatus.Partial);

        Assert.Throws<ArgumentException>(() => new ParkHistoricalSnapshot(
            "park-1",
            HistoricalInstant.ForDay(2000, 1, 1),
            new[] { subjectSnapshot },
            emptyCoverage,
            Array.Empty<HistoricalAmbiguity>(),
            "test-v1"));
    }

    [Fact]
    public void Constructor_WithAmbiguityForAnotherSubject_Throws()
    {
        HistoricalSubjectSnapshot subjectSnapshot = CreateSubjectSnapshot(Subject);
        HistoricalCoverage coverage = new HistoricalCoverage(
            1,
            1,
            0,
            0,
            new HistoricalFieldCoverage(0, 1),
            new HistoricalFieldCoverage(0, 1),
            null,
            HistoricalCoverageStatus.Partial);
        HistoricalSubject foreignSubject = new HistoricalSubject(
            HistoricalSubjectType.ParkItem,
            "item-2",
            "Autre élément",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject);
        HistoricalAmbiguity ambiguity = new HistoricalAmbiguity(
            foreignSubject,
            HistoricalSnapshotReasonCode.NoEligibleLifecycleFact,
            null,
            Array.Empty<Guid>());

        Assert.Throws<ArgumentException>(() => new ParkHistoricalSnapshot(
            "park-1",
            HistoricalInstant.ForDay(2000, 1, 1),
            new[] { subjectSnapshot },
            coverage,
            new[] { ambiguity },
            "test-v1"));
    }

    private static HistoricalSubjectSnapshot CreateSubjectSnapshot(HistoricalSubject subject)
    {
        return new HistoricalSubjectSnapshot(
            subject,
            HistoricalOperationalState.KnownClosed,
            HistoricalPresenceExtent.None,
            Array.Empty<HistoricalPresenceInterval>(),
            Array.Empty<HistoricalAttributeSnapshot>(),
            Array.Empty<HistoricalSnapshotReason>(),
            Array.Empty<Guid>());
    }
}
