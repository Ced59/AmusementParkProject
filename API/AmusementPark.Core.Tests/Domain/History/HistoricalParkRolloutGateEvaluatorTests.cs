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
            new[] { 1998 },
            SourceKeys(majorFact, supportingFact));

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
            Array.Empty<int>(),
            SourceKeys(majorFact, supportingFact));

        Assert.False(result.IsOpen);
        Assert.True(result.HasEnoughStructuredFacts);
        Assert.True(result.HasCompleteSourceCoverage);
        Assert.True(result.HasMajorMilestone);
        Assert.False(result.HasIndexableKeyYear);
    }

    [Fact]
    public void Evaluate_WithOnlyOneStructuredFact_ShouldKeepParkClosed()
    {
        HistoricalFact fact = CreateFact(1998, HistoricalImportance.Major);
        HistoricalParkRolloutGate result = new HistoricalParkRolloutGateEvaluator().Evaluate(
            new[] { fact },
            new[] { 1998 },
            SourceKeys(fact));

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
        HistoricalFact firstFact = CreateFact(1998, HistoricalImportance.Standard);
        HistoricalFact secondFact = CreateFact(1990, HistoricalImportance.Standard);
        HistoricalParkRolloutGate result = new HistoricalParkRolloutGateEvaluator().Evaluate(
            new[] { firstFact, secondFact },
            new[] { 1998 },
            SourceKeys(firstFact, secondFact));

        Assert.False(result.IsOpen);
        Assert.False(result.HasMajorMilestone);
        Assert.Equal(0, result.MajorFactCount);
    }

    [Fact]
    public void Evaluate_WhenDisputedFactLosesCurrentSupportingEvidence_ShouldKeepParkClosed()
    {
        HistoricalFact disputedFact = CreateDisputedFact(1998, HistoricalImportance.Major);
        HistoricalFact supportingFact = CreateFact(1990, HistoricalImportance.Standard);
        HistoricalSourceRevisionReference contradictingReference = disputedFact.SourceReferences
            .Single(static reference => reference.Position == HistoricalEvidencePosition.Contradicts);
        HashSet<(Guid SourceId, int Revision)> admissibleSources = SourceKeys(supportingFact)
            .Append((contradictingReference.SourceId, contradictingReference.Revision))
            .ToHashSet();

        HistoricalParkRolloutGate result = new HistoricalParkRolloutGateEvaluator().Evaluate(
            new[] { disputedFact, supportingFact },
            new[] { 1998 },
            admissibleSources);

        Assert.False(result.IsOpen);
        Assert.False(result.HasCompleteSourceCoverage);
        Assert.Equal(1, result.SourcedFactCount);
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

    private static HistoricalFact CreateDisputedFact(int year, HistoricalImportance importance)
    {
        HistoricalFact supportingFact = CreateFact(year, importance);
        HistoricalSourceRevisionReference supportingReference = supportingFact.SourceReferences.Single();
        HistoricalSourceRevisionReference contradictingReference = new(
            Guid.NewGuid(),
            1,
            supportingFact.Subject.Type,
            supportingFact.Subject.Id,
            supportingFact.Type,
            supportingFact.Period,
            HistoricalEvidencePosition.Contradicts,
            supportingReference.Scopes,
            supportingReference.HistoricalLabel,
            supportingReference.StructuredValue,
            supportingReference.SequenceWithinDate,
            supportingReference.NarrativeContentId,
            supportingReference.OtherTypeLabel,
            supportingReference.LifecycleBoundaryMeaning,
            supportingReference.AttributeKind,
            supportingReference.AttributeBoundaryMeaning);
        return new HistoricalFact(
            supportingFact.Id,
            supportingFact.Subject,
            supportingFact.Type,
            supportingFact.Period,
            HistoricalFactState.Disputed,
            supportingFact.Importance,
            supportingFact.WorkflowState,
            supportingFact.PublicationState,
            HistoricalLocalizationPolicy.SupportedLanguageCodes
                .Select(static languageCode => new HistoricalLocalizedText(
                    languageCode,
                    "Les sources admissibles se contredisent."))
                .ToArray(),
            supportingFact.LifecycleBoundaryMeaning,
            supportingFact.AttributeKind,
            supportingFact.AttributeBoundaryMeaning,
            supportingFact.SequenceWithinDate,
            new[] { supportingReference, contradictingReference },
            supportingFact.StructuredValue,
            supportingFact.OtherTypeLabel,
            supportingFact.NarrativeContentId,
            null,
            supportingFact.PublishedAtUtc,
            supportingFact.PublicationMethodologyVersion,
            supportingFact.Revision,
            supportingFact.SupersedesRevision,
            supportingFact.RecordedAtUtc,
            supportingFact.RevisionOrigin);
    }

    private static IReadOnlySet<(Guid SourceId, int Revision)> SourceKeys(
        params HistoricalFact[] facts)
    {
        return facts
            .SelectMany(static fact => fact.SourceReferences)
            .Select(static reference => (reference.SourceId, reference.Revision))
            .ToHashSet();
    }
}
