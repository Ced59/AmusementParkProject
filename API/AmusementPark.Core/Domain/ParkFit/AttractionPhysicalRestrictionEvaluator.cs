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
                heightConditions[0]);
            return;
        }

        if (!profile.HeightCentimeters.HasValue)
        {
            context.AddUnknown(
                AttractionCompatibilityReasonCode.HeightMissing,
                heightConditions[0]);
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
            AttractionAccompanimentEvaluator.Evaluate(profile, accompaniedMinimums, context);
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
        selectedCondition = null;
        selectedCentimeters = selectHighest ? double.MinValue : double.MaxValue;
        foreach (AttractionAccessCondition condition in conditions)
        {
            if (!AttractionHeightUnitConverter.TryConvertToCentimeters(
                    condition,
                    out double centimeters))
            {
                continue;
            }

            bool shouldSelect = selectedCondition is null
                || (selectHighest && centimeters > selectedCentimeters)
                || (!selectHighest && centimeters < selectedCentimeters);
            if (shouldSelect)
            {
                selectedCondition = condition;
                selectedCentimeters = centimeters;
            }
        }

        return selectedCondition is not null;
    }

    private static AttractionAccessCondition? SelectHighestAgeThreshold(
        IEnumerable<AttractionAccessCondition> conditions)
    {
        return conditions
            .OrderByDescending(static condition => condition.Value)
            .ThenBy(static condition => condition.Type)
            .FirstOrDefault();
    }
}
