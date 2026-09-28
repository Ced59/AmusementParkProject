using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalParkRolloutGateEvaluatorTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Evaluate_WithDocumentedKeyYear_ShouldOpenParkWithoutVisitInput()
    {
        HistoricalFact majorFact = CreateFact(1998, HistoricalImportance.Major);
        HistoricalFact supportingFact = CreateFact(1990, HistoricalImportance.Standard);

        HistoricalParkRolloutGate result = new HistoricalParkRolloutGateEvaluator().Evaluate(
            new[] { majorFact, supportingFact },
            new[] { 1998 });

        Assert.True(result.IsOpen);
        Assert.Equal(2, result.PublishedFactCount);
        Assert.Equal(2, result.SourcedFactCount);
        Assert.Equal(1, result.MajorFactCount);
        Assert.Equal(new[] { 1998 }, result.IndexableKeyYears);
    }

    [Fact]
    public void Evaluate_WithoutIndexableKeyYear_ShouldKeepParkClosed()
    {
        HistoricalFact majorFact = CreateFact(1998, HistoricalImportance.Major);
        HistoricalFact supportingFact = CreateFact(1990, HistoricalImportance.Standard);

        HistoricalParkRolloutGate result = new HistoricalParkRolloutGateEvaluator().Evaluate(
            new[] { majorFact, supportingFact },
            Array.Empty<int>());

        Assert.False(result.IsOpen);
        Assert.True(result.HasEnoughStructuredFacts);
        Assert.True(result.HasCompleteSourceCoverage);
        Assert.True(result.HasMajorMilestone);
        Assert.False(result.HasIndexableKeyYear);
    }

    [Fact]
    public void Evaluate_WithOnlyOneStructuredFact_ShouldKeepParkClosed()
    {
        HistoricalParkRolloutGate result = new HistoricalParkRolloutGateEvaluator().Evaluate(
            new[] { CreateFact(1998, HistoricalImportance.Major) },
            new[] { 1998 });

        Assert.False(result.IsOpen);
        Assert.False(result.HasEnoughStructuredFacts);
    }

    [Fact]
    public void RolloutGate_WhenSourceCoverageIsIncomplete_ShouldRemainClosed()
    {
        HistoricalParkRolloutGate result = new(2, 1, 1, new[] { 1998 });

        Assert.False(result.IsOpen);
        Assert.False(result.HasCompleteSourceCoverage);
        Assert.Equal(1, result.SourcedFactCount);
    }

    [Fact]
    public void Evaluate_WithoutMajorMilestone_ShouldKeepParkClosed()
    {
        HistoricalParkRolloutGate result = new HistoricalParkRolloutGateEvaluator().Evaluate(
            new[]
            {
                CreateFact(1998, HistoricalImportance.Standard),
                CreateFact(1990, HistoricalImportance.Standard),
            },
            new[] { 1998 });

        Assert.False(result.IsOpen);
        Assert.False(result.HasMajorMilestone);
        Assert.Equal(0, result.MajorFactCount);
    }

    private static HistoricalFact CreateFact(int year, HistoricalImportance importance)
    {
        HistoricalSubject subject = new(
            HistoricalSubjectType.Park,
            "park-1",
            "Parc exemple",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");
        HistoricalPeriod period = HistoricalPeriod.Point(HistoricalDate.ForYear(year));
        HistoricalSourceRevisionReference source = new(
            Guid.NewGuid(),
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
