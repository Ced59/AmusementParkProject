using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Évalue les seuils physiques connus en conservant les tranches ambiguës.
/// </summary>
internal static class AttractionPhysicalRestrictionEvaluator
{
    public static void EvaluateHeight(
        ParkFitMemberProfile profile,
        IReadOnlyCollection<AttractionAccessCondition> conditions,
        AttractionCompatibilityEvaluationContext context)
    {
        List<AttractionAccessCondition> heightConditions = conditions
            .Where(static condition => condition.Type is AttractionAccessConditionType.MinHeight
                or AttractionAccessConditionType.MinHeightAccompanied
                or AttractionAccessConditionType.MaxHeight)
            .ToList();
        if (heightConditions.Count == 0)
        {
            return;
        }

        if (AttractionHeightRangeConsistencyEvaluator.HasContradiction(heightConditions))
        {
            context.AddUnknown(
                AttractionCompatibilityReasonCode.ConflictingConditions,
                SelectStableCondition(heightConditions));
            return;
        }

        if (!profile.HeightCentimeters.HasValue)
        {
            context.AddUnknown(
                AttractionCompatibilityReasonCode.HeightMissing,
                SelectStableCondition(heightConditions));
            return;
        }

        int heightCentimeters = profile.HeightCentimeters.Value;
        List<AttractionAccessCondition> maximums = heightConditions
            .Where(static condition => condition.Type == AttractionAccessConditionType.MaxHeight)
            .ToList();
        if (TrySelectHeightThreshold(
                maximums,
                selectHighest: false,
                out AttractionAccessCondition? maximum,
                out double maximumCentimeters))
        {
            if (heightCentimeters > maximumCentimeters)
            {
                context.AddViolation(AttractionCompatibilityReasonCode.AboveMaximumHeight, maximum!);
            }
            else
            {
                context.AddSatisfied(AttractionCompatibilityReasonCode.HeightRequirementMet, maximum!);
                EvaluateExplicitAccompaniment(profile, maximums, context);
            }
        }

        List<AttractionAccessCondition> aloneMinimums = heightConditions
            .Where(static condition => condition.Type == AttractionAccessConditionType.MinHeight
                && condition.RequiresAccompaniment != true)
            .ToList();
        List<AttractionAccessCondition> accompaniedMinimums = heightConditions
            .Where(static condition => condition.Type == AttractionAccessConditionType.MinHeightAccompanied
                || (condition.Type == AttractionAccessConditionType.MinHeight
                    && condition.RequiresAccompaniment == true))
            .ToList();
        bool hasAloneMinimum = TrySelectHeightThreshold(
            aloneMinimums,
            selectHighest: true,
            out AttractionAccessCondition? aloneMinimum,
            out double aloneMinimumCentimeters);
        bool hasAccompaniedMinimum = TrySelectHeightThreshold(
            accompaniedMinimums,
            selectHighest: true,
            out AttractionAccessCondition? accompaniedMinimum,
            out double accompaniedMinimumCentimeters);

        if (hasAloneMinimum
            && hasAccompaniedMinimum
            && accompaniedMinimumCentimeters > aloneMinimumCentimeters)
        {
            context.AddUnknown(
                AttractionCompatibilityReasonCode.ConflictingConditions,
                accompaniedMinimum!);
            return;
        }

        if (hasAloneMinimum && heightCentimeters >= aloneMinimumCentimeters)
        {
            context.AddSatisfied(
                AttractionCompatibilityReasonCode.HeightRequirementMet,
                aloneMinimum!);
            return;
        }

        if (hasAccompaniedMinimum && heightCentimeters >= accompaniedMinimumCentimeters)
        {
            context.AddSatisfied(
                AttractionCompatibilityReasonCode.HeightRequirementMet,
                accompaniedMinimum!);
            AttractionAccompanimentEvaluator.Evaluate(profile, accompaniedMinimums, context);
            return;
        }

        if (context.HasUnresolvedHeightAccompaniedAlternative
            && profile.CanBeAccompanied != false)
        {
            return;
        }

        if (hasAccompaniedMinimum)
        {
            context.AddViolation(
                AttractionCompatibilityReasonCode.BelowMinimumHeight,
                accompaniedMinimum!);
        }
        else if (hasAloneMinimum)
        {
            context.AddViolation(
                AttractionCompatibilityReasonCode.BelowMinimumHeight,
                aloneMinimum!);
        }
    }

    public static void EvaluateAge(
        ParkFitMemberProfile profile,
        IReadOnlyCollection<AttractionAccessCondition> conditions,
        AttractionCompatibilityEvaluationContext context)
    {
        List<AttractionAccessCondition> aloneMinimums = conditions
            .Where(static condition => condition.Type == AttractionAccessConditionType.MinAge
                && condition.RequiresAccompaniment != true)
            .ToList();
        List<AttractionAccessCondition> accompaniedMinimums = conditions
            .Where(static condition => condition.Type == AttractionAccessConditionType.MinAgeAccompanied
                || (condition.Type == AttractionAccessConditionType.MinAge
                    && condition.RequiresAccompaniment == true))
            .ToList();
        if (aloneMinimums.Count == 0 && accompaniedMinimums.Count == 0)
        {
            return;
        }

        AttractionAccessCondition? aloneMinimum = SelectHighestAgeThreshold(aloneMinimums);
        AttractionAccessCondition? accompaniedMinimum = SelectHighestAgeThreshold(accompaniedMinimums);
        if (aloneMinimum is not null
            && accompaniedMinimum is not null
            && accompaniedMinimum.Value!.Value > aloneMinimum.Value!.Value)
        {
            context.AddUnknown(
                AttractionCompatibilityReasonCode.ConflictingConditions,
                accompaniedMinimum);
            return;
        }

        ParkFitAgeRange? ageRange = profile.AgeRange;
        AttractionAccessCondition representative = accompaniedMinimum ?? aloneMinimum!;
        if (ageRange is null)
        {
            context.AddUnknown(
                AttractionCompatibilityReasonCode.AgeRangeMissing,
                representative);
            return;
        }

        if (aloneMinimum is not null && ageRange.MinimumYears >= aloneMinimum.Value!.Value)
        {
            context.AddSatisfied(
                AttractionCompatibilityReasonCode.AgeRequirementMet,
                aloneMinimum);
            return;
        }

        if (accompaniedMinimum is not null
            && ageRange.MinimumYears >= accompaniedMinimum.Value!.Value)
        {
            context.AddSatisfied(
                AttractionCompatibilityReasonCode.AgeRequirementMet,
                accompaniedMinimum);
            AttractionAccessCondition? uncertainAloneThreshold = aloneMinimum is not null
                && ageRange.MaximumYears >= aloneMinimum.Value!.Value
                    ? aloneMinimum
                    : null;
            AttractionAccompanimentEvaluator.Evaluate(
                profile,
                accompaniedMinimums,
                context,
                uncertainAloneThreshold);
            return;
        }

        if (context.HasUnresolvedAgeAccompaniedAlternative
            && profile.CanBeAccompanied != false)
        {
            return;
        }

        if (accompaniedMinimum is not null)
        {
            if (ageRange.MaximumYears < accompaniedMinimum.Value!.Value)
            {
                context.AddViolation(
                    AttractionCompatibilityReasonCode.BelowMinimumAge,
                    accompaniedMinimum);
            }
            else
            {
                context.AddUnknown(
                    AttractionCompatibilityReasonCode.AgeRangeCrossesThreshold,
                    accompaniedMinimum);
            }

            return;
        }

        if (ageRange.MaximumYears < aloneMinimum!.Value!.Value)
        {
            context.AddViolation(
                AttractionCompatibilityReasonCode.BelowMinimumAge,
                aloneMinimum);
        }
        else
        {
            context.AddUnknown(
                AttractionCompatibilityReasonCode.AgeRangeCrossesThreshold,
                aloneMinimum);
        }
    }

    private static void EvaluateExplicitAccompaniment(
        ParkFitMemberProfile profile,
        IReadOnlyCollection<AttractionAccessCondition> conditions,
        AttractionCompatibilityEvaluationContext context)
    {
        List<AttractionAccessCondition> requiringAccompaniment = conditions
            .Where(static condition => condition.RequiresAccompaniment == true)
            .ToList();
        AttractionAccompanimentEvaluator.Evaluate(profile, requiringAccompaniment, context);
    }

    private static bool TrySelectHeightThreshold(
        IReadOnlyCollection<AttractionAccessCondition> conditions,
        bool selectHighest,
        out AttractionAccessCondition? selectedCondition,
        out double selectedCentimeters)
    {
        List<(AttractionAccessCondition Condition, double Centimeters)> candidates =
            new List<(AttractionAccessCondition Condition, double Centimeters)>();
        foreach (AttractionAccessCondition condition in conditions)
        {
            if (!AttractionHeightUnitConverter.TryConvertToCentimeters(
                    condition,
                    out double centimeters))
            {
                continue;
            }

            candidates.Add((condition, centimeters));
        }

        if (candidates.Count == 0)
        {
            selectedCondition = null;
            selectedCentimeters = selectHighest ? double.MinValue : double.MaxValue;
            return false;
        }

        IOrderedEnumerable<(AttractionAccessCondition Condition, double Centimeters)> ordered =
            selectHighest
                ? candidates.OrderByDescending(static candidate => candidate.Centimeters)
                : candidates.OrderBy(static candidate => candidate.Centimeters);
        (AttractionAccessCondition Condition, double Centimeters) selected = ordered
            .ThenByDescending(static candidate => candidate.Condition.MinimumCompanionAge ?? -1)
            .ThenBy(static candidate => candidate.Condition.Type)
            .ThenBy(static candidate => candidate.Condition.Unit)
            .ThenBy(static candidate => candidate.Condition.Value)
            .ThenBy(static candidate => candidate.Condition.RequiresAccompaniment)
            .ThenBy(static candidate => candidate.Condition.Scope)
            .ThenBy(static candidate => candidate.Condition.ScopeDetail, StringComparer.Ordinal)
            .ThenBy(static candidate => candidate.Condition.SourceKind)
            .ThenBy(static candidate => candidate.Condition.SourceUrl, StringComparer.Ordinal)
            .ThenBy(static candidate => candidate.Condition.SourceReference, StringComparer.Ordinal)
            .ThenBy(static candidate => candidate.Condition.SourceLanguageCode, StringComparer.Ordinal)
            .ThenBy(static candidate => candidate.Condition.EffectiveFrom)
            .ThenBy(static candidate => candidate.Condition.EffectiveTo)
            .First();
        selectedCondition = selected.Condition;
        selectedCentimeters = selected.Centimeters;
        return true;
    }

    private static AttractionAccessCondition? SelectHighestAgeThreshold(
        IEnumerable<AttractionAccessCondition> conditions)
    {
        return conditions
            .OrderByDescending(static condition => condition.Value)
            .ThenByDescending(static condition => condition.MinimumCompanionAge ?? -1)
            .ThenBy(static condition => condition.Type)
            .ThenBy(static condition => condition.Unit)
            .ThenBy(static condition => condition.RequiresAccompaniment)
            .ThenBy(static condition => condition.Scope)
            .ThenBy(static condition => condition.ScopeDetail, StringComparer.Ordinal)
            .ThenBy(static condition => condition.SourceKind)
            .ThenBy(static condition => condition.SourceUrl, StringComparer.Ordinal)
            .ThenBy(static condition => condition.SourceReference, StringComparer.Ordinal)
            .ThenBy(static condition => condition.SourceLanguageCode, StringComparer.Ordinal)
            .ThenBy(static condition => condition.EffectiveFrom)
            .ThenBy(static condition => condition.EffectiveTo)
            .FirstOrDefault();
    }

    private static AttractionAccessCondition SelectStableCondition(
        IEnumerable<AttractionAccessCondition> conditions)
    {
        return conditions
            .OrderBy(static condition => condition.Type)
            .ThenBy(static condition => condition.Value)
            .ThenBy(static condition => condition.Unit)
            .ThenByDescending(static condition => condition.MinimumCompanionAge ?? -1)
            .ThenBy(static condition => condition.RequiresAccompaniment)
            .ThenBy(static condition => condition.Scope)
            .ThenBy(static condition => condition.ScopeDetail, StringComparer.Ordinal)
            .ThenBy(static condition => condition.SourceKind)
            .ThenBy(static condition => condition.SourceUrl, StringComparer.Ordinal)
            .ThenBy(static condition => condition.SourceReference, StringComparer.Ordinal)
            .ThenBy(static condition => condition.SourceLanguageCode, StringComparer.Ordinal)
            .ThenBy(static condition => condition.EffectiveFrom)
            .ThenBy(static condition => condition.EffectiveTo)
            .First();
    }
}
