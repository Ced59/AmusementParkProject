namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Audite les faits FIT d'une attraction sans dépendance extérieure.
/// </summary>
internal sealed class ParkFitDataQualityItemAssessor
{
    private static readonly IReadOnlySet<AttractionAccessConditionEvidenceIssue> SourceIssues =
        new HashSet<AttractionAccessConditionEvidenceIssue>
        {
            AttractionAccessConditionEvidenceIssue.SourceNotDecisionEligible,
            AttractionAccessConditionEvidenceIssue.MissingSourceReference,
            AttractionAccessConditionEvidenceIssue.InvalidSourceUrl,
            AttractionAccessConditionEvidenceIssue.MissingSourceLanguage,
            AttractionAccessConditionEvidenceIssue.InvalidSourceLanguage,
            AttractionAccessConditionEvidenceIssue.MissingSourceSummary,
            AttractionAccessConditionEvidenceIssue.InsufficientConfidence,
        };

    private static readonly IReadOnlySet<AttractionAccessConditionEvidenceIssue> TimestampIssues =
        new HashSet<AttractionAccessConditionEvidenceIssue>
        {
            AttractionAccessConditionEvidenceIssue.MissingCollectionTimestamp,
            AttractionAccessConditionEvidenceIssue.MissingVerificationTimestamp,
            AttractionAccessConditionEvidenceIssue.TimestampNotUtc,
        };

    private static readonly IReadOnlySet<AttractionAccessConditionEvidenceIssue> AmbiguityIssues =
        new HashSet<AttractionAccessConditionEvidenceIssue>
        {
            AttractionAccessConditionEvidenceIssue.UnsupportedSchemaVersion,
            AttractionAccessConditionEvidenceIssue.VerificationBeforeCollection,
            AttractionAccessConditionEvidenceIssue.VerificationInFuture,
            AttractionAccessConditionEvidenceIssue.InvalidEffectivePeriod,
            AttractionAccessConditionEvidenceIssue.InvalidScope,
            AttractionAccessConditionEvidenceIssue.MissingScopeDetail,
        };

    public ParkFitDataQualityItemAssessment Assess(
        ParkItem item,
        DateTime evaluatedAtUtc,
        TimeSpan maximumVerificationAge)
    {
        ArgumentNullException.ThrowIfNull(item);

        IReadOnlyCollection<AttractionAccessCondition> conditions = item.AttractionDetails is null
            ? Array.Empty<AttractionAccessCondition>()
            : item.AttractionDetails.AccessConditions;
        HashSet<ParkFitDataQualityIssue> issues = new HashSet<ParkFitDataQualityIssue>();
        int decisionEligibleConditionCount = 0;

        AddStructuralIssues(item, issues);
        if (conditions.Count == 0)
        {
            issues.Add(ParkFitDataQualityIssue.MissingAccessConditions);
        }

        foreach (AttractionAccessCondition condition in conditions)
        {
            IReadOnlyCollection<AttractionAccessConditionEvidenceIssue> evidenceIssues =
                AttractionAccessConditionEvidenceEvaluator.Evaluate(
                    condition,
                    evaluatedAtUtc,
                    maximumVerificationAge);
            IReadOnlyCollection<AttractionAccessConditionSemanticIssue> semanticIssues =
                AttractionAccessConditionSemanticEvaluator.Evaluate(condition);
            if (semanticIssues.Count > 0)
            {
                issues.Add(ParkFitDataQualityIssue.AmbiguousRestriction);
            }

            if (evidenceIssues.Count == 0 && semanticIssues.Count == 0)
            {
                decisionEligibleConditionCount++;
            }

            AddMappedIssues(evidenceIssues, issues);
        }

        if (AttractionHeightRangeConsistencyEvaluator.HasContradiction(conditions))
        {
            issues.Add(ParkFitDataQualityIssue.AmbiguousRestriction);
        }

        return new ParkFitDataQualityItemAssessment
        {
            ParkItemId = item.Id,
            ParkItemName = item.Name,
            ConditionCount = conditions.Count,
            DecisionEligibleConditionCount = decisionEligibleConditionCount,
            LastVerifiedAtUtc = conditions
                .Where(static condition => condition.VerifiedAtUtc?.Kind == DateTimeKind.Utc)
                .Select(static condition => condition.VerifiedAtUtc)
                .Max(),
            Issues = issues.OrderBy(static issue => issue).ToList(),
        };
    }

    private static void AddStructuralIssues(ParkItem item, ISet<ParkFitDataQualityIssue> issues)
    {
        if (item.Type is ParkItemType.Attraction or ParkItemType.Other)
        {
            issues.Add(ParkFitDataQualityIssue.MissingPreciseAttractionType);
        }

        if (item.AttractionDetails?.IsIndoor is null)
        {
            issues.Add(ParkFitDataQualityIssue.MissingIndoorOutdoorClassification);
        }

        if (item.AttractionDetails?.IsAccessibleForReducedMobility is not null
            && string.IsNullOrWhiteSpace(item.AttractionDetails.SourceUrl))
        {
            issues.Add(ParkFitDataQualityIssue.MissingAccessibilitySource);
        }
    }

    private static void AddMappedIssues(
        IReadOnlyCollection<AttractionAccessConditionEvidenceIssue> evidenceIssues,
        ISet<ParkFitDataQualityIssue> issues)
    {
        if (evidenceIssues.Any(SourceIssues.Contains))
        {
            issues.Add(ParkFitDataQualityIssue.MissingAuthoritativeSource);
        }

        if (evidenceIssues.Any(TimestampIssues.Contains))
        {
            issues.Add(ParkFitDataQualityIssue.MissingEvidenceTimestamp);
        }

        if (evidenceIssues.Contains(AttractionAccessConditionEvidenceIssue.VerificationStale))
        {
            issues.Add(ParkFitDataQualityIssue.StaleEvidence);
        }

        if (evidenceIssues.Any(AmbiguityIssues.Contains))
        {
            issues.Add(ParkFitDataQualityIssue.AmbiguousRestriction);
        }
    }

}
