namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Vérifie la cohérence des plages de taille après normalisation de leurs unités
/// et uniquement entre des conditions qui peuvent s'appliquer simultanément.
/// </summary>
internal static class AttractionHeightRangeConsistencyEvaluator
{
    private const double CentimetersPerInch = 2.54d;

    public static bool HasUnusableHeightCondition(
        IReadOnlyCollection<AttractionAccessCondition> conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        return conditions.Any(condition => IsHeightCondition(condition)
            && !TryConvertToCentimeters(condition, out double _));
    }

    public static bool HasContradiction(
        IReadOnlyCollection<AttractionAccessCondition> conditions)
    {
        ArgumentNullException.ThrowIfNull(conditions);

        List<AttractionAccessCondition> minimums = conditions
            .Where(static condition => condition.Type is AttractionAccessConditionType.MinHeight
                or AttractionAccessConditionType.MinHeightAccompanied)
            .ToList();
        List<AttractionAccessCondition> maximums = conditions
            .Where(static condition => condition.Type == AttractionAccessConditionType.MaxHeight)
            .ToList();

        foreach (AttractionAccessCondition minimum in minimums)
        {
            if (!TryConvertToCentimeters(minimum, out double minimumCentimeters))
            {
                continue;
            }

            foreach (AttractionAccessCondition maximum in maximums)
            {
                if (!CanApplyTogether(minimum, maximum)
                    || !TryConvertToCentimeters(maximum, out double maximumCentimeters))
                {
                    continue;
                }

                if (minimumCentimeters > maximumCentimeters)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryConvertToCentimeters(
        AttractionAccessCondition condition,
        out double centimeters)
    {
        centimeters = 0;
        if (!condition.Value.HasValue
            || !double.IsFinite(condition.Value.Value)
            || condition.Value.Value <= 0)
        {
            return false;
        }

        if (condition.Unit == AttractionAccessConditionUnit.Centimeter)
        {
            centimeters = condition.Value.Value;
            return true;
        }

        if (condition.Unit == AttractionAccessConditionUnit.Inch)
        {
            centimeters = condition.Value.Value * CentimetersPerInch;
            return true;
        }

        return false;
    }

    private static bool CanApplyTogether(
        AttractionAccessCondition first,
        AttractionAccessCondition second)
    {
        if (!EffectivePeriodsOverlap(first, second))
        {
            return false;
        }

        if (first.Scope == AttractionAccessConditionScope.Attraction
            || second.Scope == AttractionAccessConditionScope.Attraction)
        {
            return true;
        }

        return first.Scope == second.Scope
            && string.Equals(
                NormalizeScopeDetail(first.ScopeDetail),
                NormalizeScopeDetail(second.ScopeDetail),
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool EffectivePeriodsOverlap(
        AttractionAccessCondition first,
        AttractionAccessCondition second)
    {
        if (HasInvalidPeriod(first) || HasInvalidPeriod(second))
        {
            return false;
        }

        DateOnly firstStart = first.EffectiveFrom ?? DateOnly.MinValue;
        DateOnly firstEnd = first.EffectiveTo ?? DateOnly.MaxValue;
        DateOnly secondStart = second.EffectiveFrom ?? DateOnly.MinValue;
        DateOnly secondEnd = second.EffectiveTo ?? DateOnly.MaxValue;
        return firstStart <= secondEnd && secondStart <= firstEnd;
    }

    private static bool HasInvalidPeriod(AttractionAccessCondition condition)
    {
        return condition.EffectiveFrom.HasValue
            && condition.EffectiveTo.HasValue
            && condition.EffectiveTo.Value < condition.EffectiveFrom.Value;
    }

    private static bool IsHeightCondition(AttractionAccessCondition condition)
    {
        return condition.Type is AttractionAccessConditionType.MinHeight
            or AttractionAccessConditionType.MinHeightAccompanied
            or AttractionAccessConditionType.MaxHeight;
    }

    private static string? NormalizeScopeDetail(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
