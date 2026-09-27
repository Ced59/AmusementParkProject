using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class ParkHistoricalComparisonBuilderTests
{
    private readonly IParkHistoricalComparisonBuilder builder = new ParkHistoricalComparisonBuilder();

    [Fact]
    public void Build_WhenSnapshotsContainExactChanges_ShouldClassifyWithoutCausalInference()
    {
        ParkHistoricalSnapshot from = CreateSnapshot(
            2000,
            CreateSubject("stable", HistoricalOperationalState.KnownOpen, 2000, "Ancien nom", "zone-a", "Coaster"),
            CreateSubject("opened", HistoricalOperationalState.KnownClosed, 2000, "Nouveauté", "zone-a", "Ride"),
            CreateSubject("closed", HistoricalOperationalState.KnownOpen, 2000, "Spectacle", "zone-b", "Show"),
            CreateSubject("uncertain", HistoricalOperationalState.Unknown, 2000, "Mystère", "zone-b", null));
        ParkHistoricalSnapshot to = CreateSnapshot(
            2026,
            CreateSubject("stable", HistoricalOperationalState.KnownOpen, 2026, "Nouveau nom", "zone-b", "Coaster"),
            CreateSubject("opened", HistoricalOperationalState.KnownOpen, 2026, "Nouveauté", "zone-a", "Ride"),
            CreateSubject("closed", HistoricalOperationalState.KnownClosed, 2026, "Spectacle", "zone-b", "Show"),
            CreateSubject("uncertain", HistoricalOperationalState.PossiblyOpen, 2026, "Mystère", "zone-b", null));

        ParkHistoricalComparison comparison = this.builder.Build(from, to);

        HistoricalSubjectComparison stable = comparison.Subjects.Single(
            static subject => subject.To.Subject.Id == "stable");
        Assert.Equal(HistoricalPresenceChange.PresentAtBoth, stable.PresenceChange);
        Assert.True(stable.IsRenamed);
        Assert.True(stable.IsMoved);
        Assert.Equal(HistoricalPresenceChange.Opened, comparison.Subjects.Single(
            static subject => subject.To.Subject.Id == "opened").PresenceChange);
        Assert.Equal(HistoricalPresenceChange.Closed, comparison.Subjects.Single(
            static subject => subject.To.Subject.Id == "closed").PresenceChange);
        Assert.Equal(HistoricalPresenceChange.Uncertain, comparison.Subjects.Single(
            static subject => subject.To.Subject.Id == "uncertain").PresenceChange);
        Assert.Equal(1, comparison.CategoryNetChanges.Single(
            static change => change.Category == "Ride").NetChange);
        Assert.Equal(-1, comparison.CategoryNetChanges.Single(
            static change => change.Category == "Show").NetChange);
        Assert.DoesNotContain(
            comparison.GetType().GetProperties(),
            static property => property.Name.Contains("Cause", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Build_WhenOpenItemCategoryIsUnknown_ShouldMarkCategoryComparisonIncomplete()
    {
        ParkHistoricalSnapshot from = CreateSnapshot(
            2000,
            CreateSubject("item", HistoricalOperationalState.KnownOpen, 2000, "Attraction", "zone-a", null));
        ParkHistoricalSnapshot to = CreateSnapshot(
            2026,
            CreateSubject("item", HistoricalOperationalState.KnownOpen, 2026, "Attraction", "zone-a", "Ride"));

        ParkHistoricalComparison comparison = this.builder.Build(from, to);

        Assert.False(comparison.IsCategoryComparisonComplete);
        Assert.Equal(1, comparison.FromUnclassifiedOpenItemCount);
        Assert.Equal(0, comparison.ToUnclassifiedOpenItemCount);
    }

    [Fact]
    public void Build_WhenDatesAreReversed_ShouldRejectComparison()
    {
        ParkHistoricalSnapshot from = CreateSnapshot(
            2026,
            CreateSubject("item", HistoricalOperationalState.KnownOpen, 2026, "Attraction", "zone-a", "Ride"));
        ParkHistoricalSnapshot to = CreateSnapshot(
            2000,
            CreateSubject("item", HistoricalOperationalState.KnownOpen, 2000, "Attraction", "zone-a", "Ride"));

        Assert.Throws<ArgumentException>(() => this.builder.Build(from, to));
    }

    private static ParkHistoricalSnapshot CreateSnapshot(
        int year,
        params HistoricalSubjectSnapshot[] subjects)
    {
        HistoricalCoverage coverage = new HistoricalCoverage(
            subjects.Length,
            subjects.Length,
            0,
            0,
            new HistoricalFieldCoverage(subjects.Length, subjects.Length),
            new HistoricalFieldCoverage(subjects.Length, subjects.Length),
            null,
            HistoricalCoverageStatus.HighConfidence);
        return new ParkHistoricalSnapshot(
            "park-1",
            HistoricalInstant.ForYear(year),
            subjects,
            coverage,
            Array.Empty<HistoricalAmbiguity>(),
            "hist-snapshot-v2");
    }

    private static HistoricalSubjectSnapshot CreateSubject(
        string id,
        HistoricalOperationalState state,
        int year,
        string name,
        string zone,
        string? category)
    {
        HistoricalPresenceInterval[] intervals = state == HistoricalOperationalState.KnownOpen
            ? new[] { new HistoricalPresenceInterval(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31)) }
            : Array.Empty<HistoricalPresenceInterval>();
        List<HistoricalAttributeSnapshot> attributes = new()
        {
            CreateAttribute(HistoricalAttributeKind.Name, name),
            CreateAttribute(HistoricalAttributeKind.Zone, zone),
        };
        attributes.Add(category is null
            ? new HistoricalAttributeSnapshot(
                HistoricalAttributeKind.Category,
                HistoricalAttributeValueState.Unknown,
                null,
                Array.Empty<string>(),
                Array.Empty<HistoricalSnapshotReason>(),
                Array.Empty<Guid>())
            : CreateAttribute(HistoricalAttributeKind.Category, category));
        return new HistoricalSubjectSnapshot(
            new HistoricalSubject(
                HistoricalSubjectType.ParkItem,
                id,
                name,
                HistoricalSubjectPublicationPolicy.HistoricalOnly,
                "park-1"),
            state,
            state == HistoricalOperationalState.KnownOpen
                ? HistoricalPresenceExtent.EntireRequestedPeriod
                : HistoricalPresenceExtent.None,
            intervals,
            attributes,
            Array.Empty<HistoricalSnapshotReason>(),
            Array.Empty<Guid>());
    }

    private static HistoricalAttributeSnapshot CreateAttribute(
        HistoricalAttributeKind kind,
        string value)
    {
        return new HistoricalAttributeSnapshot(
            kind,
            HistoricalAttributeValueState.Known,
            value,
            new[] { value },
            Array.Empty<HistoricalSnapshotReason>(),
            Array.Empty<Guid>());
    }
}
