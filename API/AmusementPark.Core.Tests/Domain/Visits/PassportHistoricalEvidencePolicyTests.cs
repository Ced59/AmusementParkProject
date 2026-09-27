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

    [Fact]
    public void HasCanonicalAttributeEvidence_ShouldRequireAKnownValueAndSupportingFact()
    {
        Guid factId = Guid.NewGuid();
        HistoricalSubjectSnapshot snapshot = CreateSnapshot(
            HistoricalOperationalState.KnownClosed,
            new[] { factId },
            new[]
            {
                new HistoricalAttributeSnapshot(
                    HistoricalAttributeKind.Name,
                    HistoricalAttributeValueState.Known,
                    "Ancien nom",
                    new[] { "Ancien nom" },
                    Array.Empty<HistoricalSnapshotReason>(),
                    new[] { factId }),
                new HistoricalAttributeSnapshot(
                    HistoricalAttributeKind.Category,
                    HistoricalAttributeValueState.Known,
                    "DarkRide",
                    new[] { "DarkRide" },
                    Array.Empty<HistoricalSnapshotReason>(),
                    Array.Empty<Guid>()),
            });

        Assert.True(PassportHistoricalEvidencePolicy.HasCanonicalAttributeEvidence(
            snapshot,
            HistoricalAttributeKind.Name));
        Assert.False(PassportHistoricalEvidencePolicy.HasCanonicalAttributeEvidence(
            snapshot,
            HistoricalAttributeKind.Category));
    }

    private static HistoricalSubjectSnapshot CreateSnapshot(
        HistoricalOperationalState state,
        IReadOnlyCollection<Guid> supportingFactIds,
        IReadOnlyCollection<HistoricalAttributeSnapshot>? attributes = null)
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
            attributes ?? Array.Empty<HistoricalAttributeSnapshot>(),
            Array.Empty<HistoricalSnapshotReason>(),
            supportingFactIds);
    }
}
