using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Visits;

public sealed class PassportHistoricalEvidencePolicyTests
{
    [Fact]
    public void HasCanonicalVisitEvidence_WithKnownStateAndSupportingFact_ShouldReturnTrue()
    {
        HistoricalSubjectSnapshot snapshot = CreateSnapshot(
            HistoricalOperationalState.KnownClosed,
            new[] { Guid.NewGuid() });

        bool result = PassportHistoricalEvidencePolicy.HasCanonicalVisitEvidence(snapshot);

        Assert.True(result);
    }

    [Theory]
    [InlineData(HistoricalOperationalState.KnownClosed)]
    [InlineData(HistoricalOperationalState.PossiblyOpen)]
    [InlineData(HistoricalOperationalState.Unknown)]
    public void HasCanonicalVisitEvidence_WithoutCertainPublishedEvidence_ShouldReturnFalse(
        HistoricalOperationalState state)
    {
        HistoricalSubjectSnapshot snapshot = CreateSnapshot(
            state,
            Array.Empty<Guid>());

        bool result = PassportHistoricalEvidencePolicy.HasCanonicalVisitEvidence(snapshot);

        Assert.False(result);
    }

    private static HistoricalSubjectSnapshot CreateSnapshot(
        HistoricalOperationalState state,
        IReadOnlyCollection<Guid> supportingFactIds)
    {
        HistoricalSubject subject = new(
            HistoricalSubjectType.ParkItem,
            "item-1",
            "Attraction",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");
        return new HistoricalSubjectSnapshot(
            subject,
            state,
            HistoricalPresenceExtent.None,
            Array.Empty<HistoricalPresenceInterval>(),
            Array.Empty<HistoricalAttributeSnapshot>(),
            Array.Empty<HistoricalSnapshotReason>(),
            supportingFactIds);
    }
}
