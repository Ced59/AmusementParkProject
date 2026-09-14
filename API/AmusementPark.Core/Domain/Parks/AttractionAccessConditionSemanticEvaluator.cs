namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Vérifie qu'une condition possède les faits nécessaires pour être interprétée.
/// </summary>
public static class AttractionAccessConditionSemanticEvaluator
{
    public const int MaximumSupportedAgeYears = 130;

    public const int MaximumSupportedHeightCentimeters = 300;

    public static IReadOnlyCollection<AttractionAccessConditionSemanticIssue> Evaluate(
        AttractionAccessCondition condition)
    {
        ArgumentNullException.ThrowIfNull(condition);

        List<AttractionAccessConditionSemanticIssue> issues =
            new List<AttractionAccessConditionSemanticIssue>();
        if (!Enum.IsDefined(condition.Type))
        {
            issues.Add(AttractionAccessConditionSemanticIssue.UnsupportedType);
            return issues;
        }

        if (condition.MinimumCompanionAge.HasValue
            && condition.MinimumCompanionAge.Value is <= 0 or > MaximumSupportedAgeYears)
        {
            issues.Add(AttractionAccessConditionSemanticIssue.InvalidCompanionAge);
        }

        switch (condition.Type)
        {
            case AttractionAccessConditionType.MinHeight:
            case AttractionAccessConditionType.MinHeightAccompanied:
            case AttractionAccessConditionType.MaxHeight:
                AddNumericIssues(
                    condition,
                    static unit => unit is AttractionAccessConditionUnit.Centimeter
                        or AttractionAccessConditionUnit.Inch,
                    requireWholeValue: false,
                    maximumValue: null,
                    issues);
                AddHeightDomainIssue(condition, issues);
                break;
            case AttractionAccessConditionType.MinAge:
            case AttractionAccessConditionType.MinAgeAccompanied:
                AddNumericIssues(
                    condition,
                    static unit => unit == AttractionAccessConditionUnit.Year,
                    requireWholeValue: true,
                    maximumValue: MaximumSupportedAgeYears,
                    issues);
                break;
            case AttractionAccessConditionType.Custom:
                if (!HasStableCustomDefinition(condition))
                {
                    issues.Add(AttractionAccessConditionSemanticIssue.MissingCustomDefinition);
                }

                break;
        }

        bool typeRequiresAccompaniment = condition.Type is
            AttractionAccessConditionType.MinHeightAccompanied
            or AttractionAccessConditionType.MinAgeAccompanied;
        if ((typeRequiresAccompaniment && condition.RequiresAccompaniment == false)
            || (!typeRequiresAccompaniment
                && condition.MinimumCompanionAge.HasValue
                && condition.RequiresAccompaniment != true))
        {
            issues.Add(AttractionAccessConditionSemanticIssue.InconsistentAccompaniment);
        }

        return issues;
    }

    public static bool IsDecisionUsable(AttractionAccessCondition condition)
    {
        return Evaluate(condition).Count == 0;
    }

    private static void AddNumericIssues(
        AttractionAccessCondition condition,
        Func<AttractionAccessConditionUnit, bool> isAllowedUnit,
        bool requireWholeValue,
        double? maximumValue,
        ICollection<AttractionAccessConditionSemanticIssue> issues)
    {
        if (!condition.Value.HasValue)
        {
            issues.Add(AttractionAccessConditionSemanticIssue.MissingValue);
        }
        else if (!double.IsFinite(condition.Value.Value)
            || condition.Value.Value <= 0
            || (maximumValue.HasValue && condition.Value.Value > maximumValue.Value)
            || (requireWholeValue && condition.Value.Value != Math.Truncate(condition.Value.Value)))
        {
            issues.Add(AttractionAccessConditionSemanticIssue.InvalidValue);
        }

        if (!condition.Unit.HasValue)
        {
            issues.Add(AttractionAccessConditionSemanticIssue.MissingUnit);
        }
        else if (!Enum.IsDefined(condition.Unit.Value)
            || !isAllowedUnit(condition.Unit.Value))
        {
            issues.Add(AttractionAccessConditionSemanticIssue.InvalidUnit);
        }
    }

    private static bool HasStableCustomDefinition(AttractionAccessCondition condition)
    {
        return !string.IsNullOrWhiteSpace(condition.TypeKey)
            || !string.IsNullOrWhiteSpace(condition.CustomTypeKey)
            || condition.CustomTypeLabel.Any(static text => !string.IsNullOrWhiteSpace(text.Value));
    }

    private static void AddHeightDomainIssue(
        AttractionAccessCondition condition,
        ICollection<AttractionAccessConditionSemanticIssue> issues)
    {
        if (issues.Contains(AttractionAccessConditionSemanticIssue.InvalidValue)
            || issues.Contains(AttractionAccessConditionSemanticIssue.MissingValue)
            || issues.Contains(AttractionAccessConditionSemanticIssue.InvalidUnit)
            || issues.Contains(AttractionAccessConditionSemanticIssue.MissingUnit))
        {
            return;
        }

        if (AttractionHeightUnitConverter.TryConvertToCentimeters(
                condition,
                out double centimeters)
            && centimeters > MaximumSupportedHeightCentimeters)
        {
            issues.Add(AttractionAccessConditionSemanticIssue.InvalidValue);
        }
    }
}
