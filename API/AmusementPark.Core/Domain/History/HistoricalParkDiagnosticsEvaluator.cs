namespace AmusementPark.Core.Domain.History;

public sealed class HistoricalParkDiagnosticsEvaluator
{
    private static readonly IReadOnlySet<HistoricalRelationType> LineageRelationTypes =
        new HashSet<HistoricalRelationType>
        {
            HistoricalRelationType.RenamedTo,
            HistoricalRelationType.ReplacedBy,
            HistoricalRelationType.MovedTo,
            HistoricalRelationType.RethemedAs,
            HistoricalRelationType.SuccessorOf,
        };

    public HistoricalParkDiagnostics Evaluate(
        IReadOnlyCollection<HistoricalFact> facts,
        IReadOnlyCollection<HistoricalRelation> relations,
        IReadOnlyCollection<string> currentZoneIds)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(relations);
        ArgumentNullException.ThrowIfNull(currentZoneIds);

        HistoricalFact[] activeFacts = facts.Where(IsActive).ToArray();
        HistoricalRelation[] activeRelations = relations.Where(IsActive).ToArray();
        List<HistoricalParkDiagnosticIssue> issues = new();
        AddDateAndSourceIssues(activeFacts, activeRelations, issues);
        AddLifecycleIssues(activeFacts, issues);
        AddLineageCycleIssue(activeRelations, issues);
        AddOverlappingNameIssues(activeFacts, issues);
        AddMissingZoneIssues(activeFacts, activeRelations, currentZoneIds, issues);

        HistoricalParkDiagnosticIssue[] orderedIssues = issues
            .Distinct()
            .OrderByDescending(static issue => issue.Severity)
            .ThenBy(static issue => issue.Code)
            .ThenBy(static issue => issue.Subject?.HistoricalLabel, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static issue => issue.FactId)
            .ThenBy(static issue => issue.RelationId)
            .ToArray();

        return new HistoricalParkDiagnostics(
            facts.Count,
            relations.Count,
            orderedIssues,
            BuildDecadeCoverage(activeFacts, activeRelations),
            BuildWorkflow(facts, relations));
    }

    private static bool IsActive(HistoricalFact fact)
    {
        return fact.State != HistoricalFactState.Retracted
            && fact.PublicationState != HistoricalPublicationState.Withdrawn;
    }

    private static bool IsActive(HistoricalRelation relation)
    {
        return relation.State != HistoricalFactState.Retracted
            && relation.PublicationState != HistoricalPublicationState.Withdrawn;
    }

    private static void AddDateAndSourceIssues(
        IReadOnlyCollection<HistoricalFact> facts,
        IReadOnlyCollection<HistoricalRelation> relations,
        ICollection<HistoricalParkDiagnosticIssue> issues)
    {
        foreach (HistoricalFact fact in facts)
        {
            AddPeriodIssues(fact.Period, fact.Subject, fact.Id, null, issues);
            if (fact.SourceReferences.Count == 0)
            {
                issues.Add(new HistoricalParkDiagnosticIssue(
                    HistoricalDiagnosticCode.MissingSource,
                    HistoricalDiagnosticSeverity.Warning,
                    fact.Subject,
                    fact.Id,
                    null));
            }
        }

        foreach (HistoricalRelation relation in relations)
        {
            AddPeriodIssues(relation.Period, relation.Source, null, relation.Id, issues);
            if (relation.SourceReferences.Count == 0)
            {
                issues.Add(new HistoricalParkDiagnosticIssue(
                    HistoricalDiagnosticCode.MissingSource,
                    HistoricalDiagnosticSeverity.Warning,
                    relation.Source,
                    null,
                    relation.Id));
            }
        }
    }

    private static void AddPeriodIssues(
        HistoricalPeriod period,
        HistoricalSubject subject,
        Guid? factId,
        Guid? relationId,
        ICollection<HistoricalParkDiagnosticIssue> issues)
    {
        if (period.Start is null)
        {
            issues.Add(new HistoricalParkDiagnosticIssue(
                HistoricalDiagnosticCode.MissingStartDate,
                HistoricalDiagnosticSeverity.Warning,
                subject,
                factId,
                relationId));
        }

        if (period.End is null)
        {
            issues.Add(new HistoricalParkDiagnosticIssue(
                HistoricalDiagnosticCode.MissingEndDate,
                HistoricalDiagnosticSeverity.Warning,
                subject,
                factId,
                relationId));
        }

        if (period.Ordering == HistoricalPeriodOrdering.Ambiguous)
        {
            issues.Add(new HistoricalParkDiagnosticIssue(
                HistoricalDiagnosticCode.AmbiguousInterval,
                HistoricalDiagnosticSeverity.Warning,
                subject,
                factId,
                relationId));
        }
    }

    private static void AddLifecycleIssues(
        IReadOnlyCollection<HistoricalFact> facts,
        ICollection<HistoricalParkDiagnosticIssue> issues)
    {
        HistoricalFact[] closures = facts.Where(static fact => fact.Type is
                HistoricalFactType.Closure
                or HistoricalFactType.DefinitiveClosure)
            .ToArray();
        foreach (HistoricalFact opening in facts.Where(static fact => fact.Type == HistoricalFactType.Opening))
        {
            DateOnly? openingEarliest = opening.Period.Start?.GetEnvelope().EarliestPossibleDate;
            if (!openingEarliest.HasValue)
            {
                continue;
            }

            bool followsClosure = closures.Any(closure =>
                SameSubject(opening.Subject, closure.Subject)
                && closure.Period.End?.GetEnvelope().LatestPossibleDate is DateOnly closureLatest
                && closureLatest < openingEarliest.Value);
            if (followsClosure)
            {
                issues.Add(new HistoricalParkDiagnosticIssue(
                    HistoricalDiagnosticCode.OpeningAfterClosure,
                    HistoricalDiagnosticSeverity.Error,
                    opening.Subject,
                    opening.Id,
                    null));
            }
        }
    }

    private static void AddLineageCycleIssue(
        IReadOnlyCollection<HistoricalRelation> relations,
        ICollection<HistoricalParkDiagnosticIssue> issues)
    {
        HistoricalRelation[] lineageRelations = relations
            .Where(relation => LineageRelationTypes.Contains(relation.Type))
            .ToArray();
        if (!HistoricalLineageCycleDetector.HasDirectedCycle(lineageRelations))
        {
            return;
        }

        issues.Add(new HistoricalParkDiagnosticIssue(
            HistoricalDiagnosticCode.IncompatibleLineageCycle,
            HistoricalDiagnosticSeverity.Error,
            null,
            null,
            null));
    }

    private static void AddOverlappingNameIssues(
        IReadOnlyCollection<HistoricalFact> facts,
        ICollection<HistoricalParkDiagnosticIssue> issues)
    {
        foreach (IGrouping<HistoricalSubjectKey, HistoricalFact> group in facts
                     .Where(static fact => fact.Type is HistoricalFactType.Renaming
                         or HistoricalFactType.ZoneRenaming)
                     .GroupBy(static fact => new HistoricalSubjectKey(
                         fact.Subject.Type,
                         fact.Subject.Id,
                         fact.Subject.ContextParkId)))
        {
            HistoricalFact[] changes = group.OrderBy(static fact => fact.Id).ToArray();
            for (int leftIndex = 0; leftIndex < changes.Length; leftIndex++)
            {
                HistoricalFact left = changes[leftIndex];
                for (int rightIndex = leftIndex + 1; rightIndex < changes.Length; rightIndex++)
                {
                    HistoricalFact right = changes[rightIndex];
                    if (string.Equals(left.StructuredValue, right.StructuredValue, StringComparison.OrdinalIgnoreCase)
                        || HasExplicitSameDayOrder(left, right)
                        || !left.Period.GetPossibleEnvelope().Overlaps(right.Period.GetPossibleEnvelope()))
                    {
                        continue;
                    }

                    issues.Add(new HistoricalParkDiagnosticIssue(
                        HistoricalDiagnosticCode.OverlappingNames,
                        HistoricalDiagnosticSeverity.Warning,
                        right.Subject,
                        right.Id,
                        null));
                }
            }
        }
    }

    private static bool HasExplicitSameDayOrder(HistoricalFact left, HistoricalFact right)
    {
        return left.SequenceWithinDate.HasValue
            && right.SequenceWithinDate.HasValue
            && left.SequenceWithinDate != right.SequenceWithinDate
            && left.Period.Start?.GetEnvelope().IsExactDay == true
            && right.Period.Start?.GetEnvelope().IsExactDay == true
            && left.Period.Start.GetEnvelope().EarliestPossibleDate
                == right.Period.Start.GetEnvelope().EarliestPossibleDate;
    }

    private static void AddMissingZoneIssues(
        IReadOnlyCollection<HistoricalFact> facts,
        IReadOnlyCollection<HistoricalRelation> relations,
        IReadOnlyCollection<string> currentZoneIds,
        ICollection<HistoricalParkDiagnosticIssue> issues)
    {
        HashSet<string> knownZoneIds = currentZoneIds
            .Where(static zoneId => !string.IsNullOrWhiteSpace(zoneId))
            .Select(static zoneId => zoneId.Trim())
            .ToHashSet(StringComparer.Ordinal);
        knownZoneIds.UnionWith(facts
            .Where(static fact => fact.Subject.Type == HistoricalSubjectType.ParkZone)
            .Select(static fact => fact.Subject.Id));

        foreach (HistoricalRelation relation in relations.Where(relation =>
                     relation.Type == HistoricalRelationType.LocatedInZoneDuring
                     && relation.Target.Type == HistoricalSubjectType.ParkZone
                     && !knownZoneIds.Contains(relation.Target.Id)))
        {
            issues.Add(new HistoricalParkDiagnosticIssue(
                HistoricalDiagnosticCode.MissingZone,
                HistoricalDiagnosticSeverity.Error,
                relation.Target,
                null,
                relation.Id));
        }
    }

    private static IReadOnlyCollection<HistoricalDecadeCoverage> BuildDecadeCoverage(
        IReadOnlyCollection<HistoricalFact> facts,
        IReadOnlyCollection<HistoricalRelation> relations)
    {
        IEnumerable<(int Decade, string ResourceKey, bool Sourced, bool Published, HistoricalSubjectKey Subject)>
            factEntries = facts.SelectMany(fact => ResolveDecades(fact.Period).Select(decade => (
                decade,
                $"fact:{fact.Id:N}",
                fact.SourceReferences.Count > 0,
                fact.PublicationState == HistoricalPublicationState.Published,
                new HistoricalSubjectKey(fact.Subject.Type, fact.Subject.Id, fact.Subject.ContextParkId))));
        IEnumerable<(int Decade, string ResourceKey, bool Sourced, bool Published, HistoricalSubjectKey Subject)>
            relationEntries = relations.SelectMany(relation => ResolveDecades(relation.Period).Select(decade => (
                decade,
                $"relation:{relation.Id:N}",
                relation.SourceReferences.Count > 0,
                relation.PublicationState == HistoricalPublicationState.Published,
                new HistoricalSubjectKey(
                    relation.Source.Type,
                    relation.Source.Id,
                    relation.Source.ContextParkId))));

        return factEntries.Concat(relationEntries)
            .GroupBy(static entry => entry.Decade)
            .OrderBy(static group => group.Key)
            .Select(static group =>
            {
                (int Decade, string ResourceKey, bool Sourced, bool Published, HistoricalSubjectKey Subject)[]
                    entries = group.DistinctBy(static entry => entry.ResourceKey).ToArray();
                return new HistoricalDecadeCoverage(
                    group.Key,
                    entries.Length,
                    entries.Count(static entry => entry.Sourced),
                    entries.Count(static entry => entry.Published),
                    entries.Select(static entry => entry.Subject).Distinct().Count());
            })
            .ToArray();
    }

    private static IEnumerable<int> ResolveDecades(HistoricalPeriod period)
    {
        int? firstYear = period.Start?.Year ?? period.End?.Year;
        int? lastYear = period.End?.Year ?? period.Start?.Year;
        if (!firstYear.HasValue || !lastYear.HasValue)
        {
            return Array.Empty<int>();
        }

        int firstDecade = firstYear.Value / 10 * 10;
        int lastDecade = lastYear.Value / 10 * 10;
        return Enumerable.Range(0, (lastDecade - firstDecade) / 10 + 1)
            .Select(index => firstDecade + index * 10);
    }

    private static IReadOnlyCollection<HistoricalWorkflowStageCount> BuildWorkflow(
        IReadOnlyCollection<HistoricalFact> facts,
        IReadOnlyCollection<HistoricalRelation> relations)
    {
        HistoricalEditorialWorkflowState[] stages = Enum.GetValues<HistoricalEditorialWorkflowState>();
        return stages.Select(stage => new HistoricalWorkflowStageCount(
                stage,
                facts.Count(fact => fact.WorkflowState == stage)
                    + relations.Count(relation => relation.WorkflowState == stage)))
            .ToArray();
    }

    private static bool SameSubject(HistoricalSubject left, HistoricalSubject right)
    {
        return left.Type == right.Type
            && string.Equals(left.Id, right.Id, StringComparison.Ordinal);
    }
}
