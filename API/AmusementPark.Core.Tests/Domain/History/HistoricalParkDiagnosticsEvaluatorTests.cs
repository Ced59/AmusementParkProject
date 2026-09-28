using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalParkDiagnosticsEvaluatorTests
{
    private static readonly DateTime RecordedAtUtc = new(2026, 9, 28, 6, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Evaluate_WithIncompleteAndContradictoryFacts_ShouldExposeActionableIssues()
    {
        HistoricalSubject subject = CreateSubject(HistoricalSubjectType.ParkItem, "item-1", "Le Carrousel");
        HistoricalFact undatedEnd = CreateDraftFact(
            subject,
            HistoricalFactType.MajorEvent,
            HistoricalPeriod.From(HistoricalDate.ForYear(1985)));
        HistoricalFact closure = CreateDraftFact(
            subject,
            HistoricalFactType.DefinitiveClosure,
            HistoricalPeriod.Point(HistoricalDate.ForDay(1990, 8, 2)));
        HistoricalFact invalidOpening = CreateDraftFact(
            subject,
            HistoricalFactType.Opening,
            HistoricalPeriod.Point(HistoricalDate.ForDay(2000, 3, 1)));

        HistoricalParkDiagnostics result = new HistoricalParkDiagnosticsEvaluator().Evaluate(
            new[] { undatedEnd, closure, invalidOpening },
            Array.Empty<HistoricalRelation>(),
            Array.Empty<string>());

        Assert.Contains(result.Issues, static issue =>
            issue.Code == HistoricalDiagnosticCode.MissingEndDate);
        Assert.Contains(result.Issues, issue =>
            issue.Code == HistoricalDiagnosticCode.OpeningAfterClosure
            && issue.FactId == invalidOpening.Id
            && issue.Severity == HistoricalDiagnosticSeverity.Error);
        Assert.Equal(1, result.BlockingIssueCount);
        Assert.Contains(result.DecadeCoverage, static coverage => coverage.Decade == 1980);
        Assert.Contains(result.DecadeCoverage, static coverage => coverage.Decade == 2000);
    }

    [Fact]
    public void Evaluate_WithOpeningAfterTemporaryClosure_ShouldExposeLifecycleBlocker()
    {
        HistoricalSubject subject = CreateSubject(HistoricalSubjectType.ParkItem, "item-1", "Le Carrousel");
        HistoricalFact temporaryClosure = CreateDraftFact(
            subject,
            HistoricalFactType.TemporaryClosure,
            HistoricalPeriod.Point(HistoricalDate.ForDay(2000, 3, 1)));
        HistoricalFact invalidOpening = CreateDraftFact(
            subject,
            HistoricalFactType.Opening,
            HistoricalPeriod.Point(HistoricalDate.ForDay(2000, 3, 2)));

        HistoricalParkDiagnostics result = new HistoricalParkDiagnosticsEvaluator().Evaluate(
            new[] { temporaryClosure, invalidOpening },
            Array.Empty<HistoricalRelation>(),
            Array.Empty<string>());

        Assert.Contains(result.Issues, issue =>
            issue.Code == HistoricalDiagnosticCode.OpeningAfterClosure
            && issue.FactId == invalidOpening.Id);
    }

    [Fact]
    public void Evaluate_WithSequencedSameDayOpeningAfterClosure_ShouldExposeLifecycleBlocker()
    {
        HistoricalSubject subject = CreateSubject(HistoricalSubjectType.ParkItem, "item-1", "Le Carrousel");
        HistoricalPeriod date = HistoricalPeriod.Point(HistoricalDate.ForDay(2000, 3, 1));
        HistoricalFact closure = CreateDraftFact(
            subject,
            HistoricalFactType.Closure,
            date,
            sequenceWithinDate: 1);
        HistoricalFact invalidOpening = CreateDraftFact(
            subject,
            HistoricalFactType.Opening,
            date,
            sequenceWithinDate: 2);

        HistoricalParkDiagnostics result = new HistoricalParkDiagnosticsEvaluator().Evaluate(
            new[] { closure, invalidOpening },
            Array.Empty<HistoricalRelation>(),
            Array.Empty<string>());

        Assert.Contains(result.Issues, issue =>
            issue.Code == HistoricalDiagnosticCode.OpeningAfterClosure
            && issue.FactId == invalidOpening.Id);
    }

    [Fact]
    public void Evaluate_WithCyclicLineageAndUnknownZone_ShouldExposeBothBlockers()
    {
        HistoricalRelation first = HistoricalRelationTests.CreatePublishedRelation(
            sourceId: "item-1",
            targetId: "item-2");
        HistoricalRelation second = HistoricalRelationTests.CreatePublishedRelation(
            sourceId: "item-2",
            targetId: "item-1");
        HistoricalSubject item = CreateSubject(HistoricalSubjectType.ParkItem, "item-3", "La Rivière");
        HistoricalSubject missingZone = CreateSubject(HistoricalSubjectType.ParkZone, "zone-missing", "Zone disparue");
        HistoricalRelation zoneRelation = CreateDraftRelation(
            item,
            missingZone,
            HistoricalRelationType.LocatedInZoneDuring,
            HistoricalRelationDirection.Directed);

        HistoricalParkDiagnostics result = new HistoricalParkDiagnosticsEvaluator().Evaluate(
            Array.Empty<HistoricalFact>(),
            new[] { first, second, zoneRelation },
            new[] { "zone-current" });

        Assert.Contains(result.Issues, static issue =>
            issue.Code == HistoricalDiagnosticCode.IncompatibleLineageCycle);
        Assert.Contains(result.Issues, issue =>
            issue.Code == HistoricalDiagnosticCode.MissingZone
            && issue.RelationId == zoneRelation.Id
            && issue.Subject?.HistoricalLabel == "Zone disparue");
        Assert.Equal(2, result.BlockingIssueCount);
    }

    [Fact]
    public void Evaluate_WithConflictingCoarseRenamings_ShouldReportOverlap()
    {
        HistoricalSubject subject = CreateSubject(HistoricalSubjectType.ParkItem, "item-1", "Attraction");
        HistoricalFact first = CreateDraftFact(
            subject,
            HistoricalFactType.Renaming,
            HistoricalPeriod.Point(HistoricalDate.ForYear(1999)),
            "Premier nom");
        HistoricalFact second = CreateDraftFact(
            subject,
            HistoricalFactType.Renaming,
            HistoricalPeriod.Point(HistoricalDate.ForYear(1999)),
            "Second nom");

        HistoricalParkDiagnostics result = new HistoricalParkDiagnosticsEvaluator().Evaluate(
            new[] { first, second },
            Array.Empty<HistoricalRelation>(),
            Array.Empty<string>());

        HistoricalParkDiagnosticIssue overlap = Assert.Single(
            result.Issues,
            static issue => issue.Code == HistoricalDiagnosticCode.OverlappingNames);
        Assert.Equal("Attraction", overlap.Subject?.HistoricalLabel);
    }

    [Fact]
    public void Evaluate_WithReversedNominalYearsInAmbiguousPeriod_ShouldBuildCoverageWithoutThrowing()
    {
        HistoricalSubject subject = CreateSubject(HistoricalSubjectType.Park, "park-1", "Parc");
        HistoricalPeriod ambiguousPeriod = new(
            HistoricalDate.ForYear(2020, qualifier: DateQualifier.Before),
            HistoricalDate.ForYear(1990, qualifier: DateQualifier.After),
            PeriodBoundaryConfidence.Confirmed,
            PeriodBoundaryConfidence.Confirmed);
        HistoricalFact fact = CreateDraftFact(
            subject,
            HistoricalFactType.MajorEvent,
            ambiguousPeriod);

        HistoricalParkDiagnostics result = new HistoricalParkDiagnosticsEvaluator().Evaluate(
            new[] { fact },
            Array.Empty<HistoricalRelation>(),
            Array.Empty<string>());

        Assert.Contains(result.Issues, static issue =>
            issue.Code == HistoricalDiagnosticCode.AmbiguousInterval);
        Assert.Equal(
            new[] { 1990, 2000, 2010, 2020 },
            result.DecadeCoverage.Select(static coverage => coverage.Decade));
    }

    private static HistoricalFact CreateDraftFact(
        HistoricalSubject subject,
        HistoricalFactType type,
        HistoricalPeriod period,
        string? structuredValue = null,
        int? sequenceWithinDate = null)
    {
        bool lifecycle = type is HistoricalFactType.Opening
            or HistoricalFactType.Closure
            or HistoricalFactType.Reopening
            or HistoricalFactType.TemporaryClosure
            or HistoricalFactType.DefinitiveClosure;
        LifecycleBoundaryMeaning? lifecycleBoundary = type switch
        {
            HistoricalFactType.Opening or HistoricalFactType.Reopening =>
                LifecycleBoundaryMeaning.FirstOperatingDay,
            HistoricalFactType.Closure
                or HistoricalFactType.TemporaryClosure
                or HistoricalFactType.DefinitiveClosure => LifecycleBoundaryMeaning.FirstClosedDay,
            _ => null,
        };
        bool nameChange = type is HistoricalFactType.Renaming or HistoricalFactType.ZoneRenaming;
        return new HistoricalFact(
            Guid.NewGuid(),
            subject,
            type,
            period,
            HistoricalFactState.Unverified,
            HistoricalImportance.Standard,
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft,
            Array.Empty<HistoricalLocalizedText>(),
            lifecycle ? lifecycleBoundary : null,
            nameChange ? HistoricalAttributeKind.Name : null,
            nameChange ? AttributeBoundaryMeaning.FirstDayOfNewValue : null,
            sequenceWithinDate,
            Array.Empty<HistoricalSourceRevisionReference>(),
            structuredValue,
            null,
            null,
            null,
            null,
            null,
            1,
            null,
            RecordedAtUtc);
    }

    private static HistoricalRelation CreateDraftRelation(
        HistoricalSubject source,
        HistoricalSubject target,
        HistoricalRelationType type,
        HistoricalRelationDirection direction)
    {
        return new HistoricalRelation(
            Guid.NewGuid(),
            source,
            target,
            type,
            direction,
            HistoricalPeriod.Point(HistoricalDate.ForYear(2001)),
            HistoricalFactState.Unverified,
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft,
            Array.Empty<HistoricalLocalizedText>(),
            Array.Empty<HistoricalRelationSourceRevisionReference>(),
            null,
            null,
            null,
            null,
            1,
            null,
            RecordedAtUtc);
    }

    private static HistoricalSubject CreateSubject(
        HistoricalSubjectType type,
        string id,
        string label)
    {
        return new HistoricalSubject(
            type,
            id,
            label,
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject,
            "park-1");
    }
}
