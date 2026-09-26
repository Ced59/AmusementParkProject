using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalAmbiguityTests
{
    private static readonly HistoricalSubject Subject = new HistoricalSubject(
        HistoricalSubjectType.ParkItem,
        "item-1",
        "Élément historique",
        HistoricalSubjectPublicationPolicy.FollowCurrentSubject);

    [Fact]
    public void Constructor_NormalizesSupportingFactIdentifiers()
    {
        Guid first = Guid.Parse("10000000-0000-0000-0000-000000000000");
        Guid second = Guid.Parse("20000000-0000-0000-0000-000000000000");

        HistoricalAmbiguity ambiguity = new HistoricalAmbiguity(
            Subject,
            HistoricalSnapshotReasonCode.AmbiguousAttributeOrder,
            HistoricalAttributeKind.Name,
            new[] { second, Guid.Empty, first, second });

        Assert.Equal(new[] { first, second }, ambiguity.FactIds);
    }

    [Fact]
    public void Constructor_WithInformationalReason_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new HistoricalAmbiguity(
            Subject,
            HistoricalSnapshotReasonCode.ConfirmedActivity,
            null,
            Array.Empty<Guid>()));
    }
}
