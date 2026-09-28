using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalSnapshotSeoEligibilityEvaluatorTests
{
    private static readonly DateTime RecordedAtUtc = new DateTime(2026, 9, 28, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void IsIndexableKeyYear_WithMajorEventContentAndSubstantialCoverage_ReturnsTrue()
    {
        HistoricalFact majorFact = CreateFact(1998, HistoricalImportance.Major);
        HistoricalFact supportingFact = CreateFact(1990, HistoricalImportance.Standard);
        ParkHistoricalSnapshot snapshot = CreateSnapshot(
            HistoricalInstant.ForYear(1998),
            HistoricalCoverageStatus.Substantial,
            majorFact.Id,
            supportingFact.Id);

        bool result = HistoricalSnapshotSeoEligibilityEvaluator.IsIndexableKeyYear(
            snapshot,
            new[] { majorFact, supportingFact });

        Assert.True(result);
    }

    [Theory]
    [InlineData(HistoryDatePrecision.Month)]
    [InlineData(HistoryDatePrecision.Day)]
    public void IsIndexableKeyYear_WithArbitraryDatePrecision_ReturnsFalse(HistoryDatePrecision precision)
    {
        HistoricalFact majorFact = CreateFact(1998, HistoricalImportance.Major);
        HistoricalFact supportingFact = CreateFact(1990, HistoricalImportance.Standard);
        HistoricalInstant instant = precision == HistoryDatePrecision.Month
            ? HistoricalInstant.ForMonth(1998, 5)
            : HistoricalInstant.ForDay(1998, 5, 12);
        ParkHistoricalSnapshot snapshot = CreateSnapshot(
            instant,
            HistoricalCoverageStatus.HighConfidence,
            majorFact.Id,
            supportingFact.Id);

        bool result = HistoricalSnapshotSeoEligibilityEvaluator.IsIndexableKeyYear(
            snapshot,
            new[] { majorFact, supportingFact });

        Assert.False(result);
    }

    [Fact]
    public void IsIndexableKeyYear_WithPartialCoverage_ReturnsFalse()
    {
        HistoricalFact majorFact = CreateFact(1998, HistoricalImportance.Major);
        HistoricalFact supportingFact = CreateFact(1990, HistoricalImportance.Standard);
        ParkHistoricalSnapshot snapshot = CreateSnapshot(
            HistoricalInstant.ForYear(1998),
            HistoricalCoverageStatus.Partial,
            majorFact.Id,
            supportingFact.Id);

        bool result = HistoricalSnapshotSeoEligibilityEvaluator.IsIndexableKeyYear(
            snapshot,
            new[] { majorFact, supportingFact });

        Assert.False(result);
    }

    [Fact]
    public void IsIndexableKeyYear_WithoutMajorBoundaryInRequestedYear_ReturnsFalse()
    {
        HistoricalFact majorFact = CreateFact(1997, HistoricalImportance.Major);
        HistoricalFact supportingFact = CreateFact(1990, HistoricalImportance.Standard);
        ParkHistoricalSnapshot snapshot = CreateSnapshot(
            HistoricalInstant.ForYear(1998),
            HistoricalCoverageStatus.HighConfidence,
            majorFact.Id,
            supportingFact.Id);

        bool result = HistoricalSnapshotSeoEligibilityEvaluator.IsIndexableKeyYear(
            snapshot,
            new[] { majorFact, supportingFact });

        Assert.False(result);
    }

    [Fact]
    public void IsIndexableKeyYear_WithoutEnoughSupportingContent_ReturnsFalse()
    {
        HistoricalFact majorFact = CreateFact(1998, HistoricalImportance.Major);
        ParkHistoricalSnapshot snapshot = CreateSnapshot(
            HistoricalInstant.ForYear(1998),
            HistoricalCoverageStatus.HighConfidence,
            majorFact.Id);

        bool result = HistoricalSnapshotSeoEligibilityEvaluator.IsIndexableKeyYear(
            snapshot,
            new[] { majorFact });

        Assert.False(result);
    }

    private static ParkHistoricalSnapshot CreateSnapshot(
        HistoricalInstant instant,
        HistoricalCoverageStatus coverageStatus,
        params Guid[] supportingFactIds)
    {
        HistoricalSubject subject = new HistoricalSubject(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc exemple",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");
        HistoricalSubjectSnapshot subjectSnapshot = new HistoricalSubjectSnapshot(
            subject,
            HistoricalOperationalState.Unknown,
            HistoricalPresenceExtent.None,
            Array.Empty<HistoricalPresenceInterval>(),
            Array.Empty<HistoricalAttributeSnapshot>(),
            Array.Empty<HistoricalSnapshotReason>(),
            supportingFactIds);
        HistoricalCoverage coverage = new HistoricalCoverage(
            1,
            1,
            0,
            0,
            new HistoricalFieldCoverage(1, 1),
            new HistoricalFieldCoverage(0, 0),
            RecordedAtUtc,
            coverageStatus);

        return new ParkHistoricalSnapshot(
            "park-1",
            instant,
            new[] { subjectSnapshot },
            coverage,
            Array.Empty<HistoricalAmbiguity>(),
            "hist-v1");
    }

    private static HistoricalFact CreateFact(int year, HistoricalImportance importance)
    {
        Guid sourceId = Guid.NewGuid();
        HistoricalSubject subject = new HistoricalSubject(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc exemple",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");
        HistoricalPeriod period = HistoricalPeriod.Point(HistoricalDate.ForYear(year));
        HistoricalSourceRevisionReference source = new HistoricalSourceRevisionReference(
            sourceId,
            1,
            subject.Type,
            subject.Id,
            HistoricalFactType.Opening,
            period,
            HistoricalEvidencePosition.Supports,
            new[]
            {
                HistoricalSourceScope.SubjectIdentity,
                HistoricalSourceScope.HistoricalLabel,
                HistoricalSourceScope.FactType,
                HistoricalSourceScope.Period,
            },
            subject.HistoricalLabel,
            null,
            null,
            null,
            null,
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null);

        return new HistoricalFact(
            Guid.NewGuid(),
            subject,
            HistoricalFactType.Opening,
            period,
            HistoricalFactState.Verified,
            importance,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            Array.Empty<HistoricalLocalizedText>(),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null,
            null,
            new[] { source },
            null,
            null,
            null,
            RecordedAtUtc.AddMinutes(-2),
            RecordedAtUtc.AddMinutes(-1),
            "hist-v1",
            2,
            1,
            RecordedAtUtc);
    }
}
