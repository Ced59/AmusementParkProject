using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Produit un verdict individuel prudent à partir des conditions canoniques.
/// </summary>
public sealed class AttractionCompatibilityEvaluator
{
    public const string MethodVersion = "park-fit-2026-01";

    public AttractionCompatibility Evaluate(
        ParkFitMemberProfile profile,
        IReadOnlyCollection<AttractionAccessCondition> conditions,
        DateOnly evaluationDate,
        DateTime evaluatedAtUtc,
        TimeSpan maximumVerificationAge)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(conditions);

        if (evaluatedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "The evaluation timestamp must use UTC.",
                nameof(evaluatedAtUtc));
        }

        if (maximumVerificationAge < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumVerificationAge));
        }

        List<AttractionAccessCondition> activeConditions = conditions
            .Where(condition => AttractionAccessConditionApplicabilityEvaluator.IsApplicableOn(
                condition,
                evaluationDate))
            .ToList();
        if (activeConditions.Count == 0)
        {
            return BuildResult(
                AttractionCompatibilityState.NotApplicable,
                new[]
                {
                    new AttractionCompatibilityReason(
                        AttractionCompatibilityReasonCode.NoApplicableCondition),
                },
                Array.Empty<AttractionAccessCondition>(),
                activeConditions,
                evaluationDate,
                evaluatedAtUtc);
        }

        AttractionCompatibilityEvaluationContext context =
            new AttractionCompatibilityEvaluationContext();
        List<AttractionAccessCondition> usableAttractionConditions =
            new List<AttractionAccessCondition>();
        foreach (AttractionAccessCondition condition in activeConditions)
        {
            IReadOnlyCollection<AttractionAccessConditionSemanticIssue> semanticIssues =
                AttractionAccessConditionSemanticEvaluator.Evaluate(condition);
            IReadOnlyCollection<AttractionAccessConditionEvidenceIssue> evidenceIssues =
                AttractionAccessConditionEvidenceEvaluator.Evaluate(
                    condition,
                    evaluatedAtUtc,
                    maximumVerificationAge);
            if (semanticIssues.Count > 0)
            {
                if (!context.TryAddUnresolvedAlternative(
                        AttractionCompatibilityReasonCode.ConditionDefinitionUnusable,
                        condition,
                        evidenceIssues,
                        semanticIssues))
                {
                    context.AddUnknown(
                        AttractionCompatibilityReasonCode.ConditionDefinitionUnusable,
                        condition,
                        evidenceIssues,
                        semanticIssues);
                }

                continue;
            }

            if (evidenceIssues.Count > 0)
            {
                if (!context.TryAddUnresolvedAlternative(
                        AttractionCompatibilityReasonCode.ConditionEvidenceUnusable,
                        condition,
                        evidenceIssues,
                        semanticIssues))
                {
                    context.AddUnknown(
                        AttractionCompatibilityReasonCode.ConditionEvidenceUnusable,
                        condition,
                        evidenceIssues,
                        semanticIssues);
                }

                continue;
            }

            if (condition.Scope != AttractionAccessConditionScope.Attraction)
            {
                if (!context.TryAddUnresolvedAlternative(
                        AttractionCompatibilityReasonCode.ScopedConditionRequiresConfiguration,
                        condition))
                {
                    context.AddUnknown(
                        AttractionCompatibilityReasonCode.ScopedConditionRequiresConfiguration,
                        condition);
                }

                continue;
            }

            usableAttractionConditions.Add(condition);
        }

        AttractionPhysicalRestrictionEvaluator.EvaluateHeight(
            profile,
            usableAttractionConditions,
            context);
        AttractionPhysicalRestrictionEvaluator.EvaluateAge(
            profile,
            usableAttractionConditions,
            context);
        AddUnsupportedPersonalRestrictions(usableAttractionConditions, context);

        AttractionCompatibilityState state = ResolveState(context);
        return BuildResult(
            state,
            context.Reasons,
            usableAttractionConditions,
            activeConditions,
            evaluationDate,
            evaluatedAtUtc);
    }

    private static void AddUnsupportedPersonalRestrictions(
        IEnumerable<AttractionAccessCondition> conditions,
        AttractionCompatibilityEvaluationContext context)
    {
        foreach (AttractionAccessCondition condition in conditions.Where(
            static condition => condition.Type is not AttractionAccessConditionType.MinHeight
                and not AttractionAccessConditionType.MinHeightAccompanied
                and not AttractionAccessConditionType.MaxHeight
                and not AttractionAccessConditionType.MinAge
                and not AttractionAccessConditionType.MinAgeAccompanied))
        {
            context.AddUnknown(
                AttractionCompatibilityReasonCode.PersonalRestrictionRequiresConfirmation,
                condition);
        }
    }

    private static AttractionCompatibilityState ResolveState(
        AttractionCompatibilityEvaluationContext context)
    {
        if (context.HasViolation)
        {
            return AttractionCompatibilityState.Incompatible;
        }

        if (context.HasUnknown)
        {
            return AttractionCompatibilityState.Unknown;
        }

        return context.RequiresCompanion
            ? AttractionCompatibilityState.CompatibleWithCompanion
            : AttractionCompatibilityState.CompatibleAlone;
    }

    private static AttractionCompatibility BuildResult(
        AttractionCompatibilityState state,
        IEnumerable<AttractionCompatibilityReason> reasons,
        IReadOnlyCollection<AttractionAccessCondition> usableConditions,
        IReadOnlyCollection<AttractionAccessCondition> activeConditions,
        DateOnly evaluationDate,
        DateTime evaluatedAtUtc)
    {
        IReadOnlyCollection<AttractionCompatibilityReason> orderedReasons = reasons
            .DistinctBy(static reason => new
            {
                reason.Code,
                reason.ConditionType,
                reason.RequiredValue,
                reason.MinimumCompanionAge,
                reason.Unit,
                reason.Scope,
                reason.ScopeDetail,
                Evidence = string.Join(',', reason.EvidenceIssues.OrderBy(static issue => issue)),
                Semantic = string.Join(',', reason.SemanticIssues.OrderBy(static issue => issue)),
            })
            .OrderBy(static reason => reason.Code)
            .ThenBy(static reason => reason.ConditionType)
            .ThenBy(static reason => reason.RequiredValue)
            .ThenBy(static reason => reason.MinimumCompanionAge)
            .ThenBy(static reason => reason.Unit)
            .ThenBy(static reason => reason.Scope)
            .ThenBy(static reason => reason.ScopeDetail, StringComparer.Ordinal)
            .ToList();
        IReadOnlyCollection<AttractionCompatibilitySourceReference> sources = activeConditions
            .Where(static condition => !string.IsNullOrWhiteSpace(condition.SourceUrl)
                || !string.IsNullOrWhiteSpace(condition.SourceReference))
            .GroupBy(static condition => new
            {
                condition.SourceKind,
                SourceUrl = Normalize(condition.SourceUrl),
                SourceReference = Normalize(condition.SourceReference),
                SourceLanguageCode = Normalize(condition.SourceLanguageCode),
                condition.CollectedAtUtc,
                CollectedAtUtcKind = condition.CollectedAtUtc?.Kind,
                condition.VerifiedAtUtc,
                VerifiedAtUtcKind = condition.VerifiedAtUtc?.Kind,
                condition.SourceConfidence,
            })
            .Select(static group => new AttractionCompatibilitySourceReference(group.ToList()))
            .OrderBy(static source => source.Kind)
            .ThenBy(static source => source.Url, StringComparer.Ordinal)
            .ThenBy(static source => source.Reference, StringComparer.Ordinal)
            .ThenBy(static source => source.LanguageCode, StringComparer.Ordinal)
            .ThenBy(static source => source.CollectedAtUtc)
            .ThenBy(static source => source.CollectedAtUtc?.Kind)
            .ThenBy(static source => source.VerifiedAtUtc)
            .ThenBy(static source => source.VerifiedAtUtc?.Kind)
            .ThenBy(static source => source.Confidence)
            .ToList();
        ParkFitDataConfidence confidence = ResolveConfidence(state, usableConditions);
        DateTime? lastVerifiedAtUtc = activeConditions
            .Where(static condition => condition.VerifiedAtUtc?.Kind == DateTimeKind.Utc)
            .Select(static condition => condition.VerifiedAtUtc)
            .Min();

        return new AttractionCompatibility
        {
            MethodVersion = MethodVersion,
            State = state,
            Reasons = orderedReasons,
            Confidence = confidence,
            LastVerifiedAtUtc = lastVerifiedAtUtc,
            EvaluatedAtUtc = evaluatedAtUtc,
            EvaluationDate = evaluationDate,
            Sources = sources,
        };
    }

    private static ParkFitDataConfidence ResolveConfidence(
        AttractionCompatibilityState state,
        IReadOnlyCollection<AttractionAccessCondition> usableConditions)
    {
        if (state is AttractionCompatibilityState.Unknown
            or AttractionCompatibilityState.NotApplicable
            || usableConditions.Count == 0)
        {
            return ParkFitDataConfidence.Unknown;
        }

        AttractionAccessConditionConfidence lowestSourceConfidence = usableConditions
            .Min(static condition => condition.SourceConfidence);
        return lowestSourceConfidence switch
        {
            AttractionAccessConditionConfidence.High => ParkFitDataConfidence.High,
            AttractionAccessConditionConfidence.Medium => ParkFitDataConfidence.Medium,
            AttractionAccessConditionConfidence.Low => ParkFitDataConfidence.Low,
            _ => ParkFitDataConfidence.Unknown,
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
