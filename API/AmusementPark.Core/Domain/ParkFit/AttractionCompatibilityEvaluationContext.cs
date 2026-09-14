using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Accumule les faits d'une décision sans appliquer la priorité finale des états.
/// </summary>
internal sealed class AttractionCompatibilityEvaluationContext
{
    private readonly List<AttractionCompatibilityReason> reasons = new List<AttractionCompatibilityReason>();

    public bool HasViolation { get; private set; }

    public bool HasUnknown { get; private set; }

    public bool RequiresCompanion { get; private set; }

    public bool HasUnresolvedHeightAccompaniedAlternative { get; private set; }

    public bool HasUnresolvedAgeAccompaniedAlternative { get; private set; }

    public IReadOnlyCollection<AttractionCompatibilityReason> Reasons => this.reasons;

    public void AddSatisfied(
        AttractionCompatibilityReasonCode code,
        AttractionAccessCondition condition)
    {
        this.reasons.Add(new AttractionCompatibilityReason(code, condition));
    }

    public void AddInformational(
        AttractionCompatibilityReasonCode code,
        AttractionAccessCondition condition)
    {
        this.reasons.Add(new AttractionCompatibilityReason(code, condition));
    }

    public void AddViolation(
        AttractionCompatibilityReasonCode code,
        AttractionAccessCondition condition)
    {
        this.HasViolation = true;
        this.reasons.Add(new AttractionCompatibilityReason(code, condition));
    }

    public void AddUnknown(
        AttractionCompatibilityReasonCode code,
        AttractionAccessCondition condition)
    {
        this.HasUnknown = true;
        this.MarkUnresolvedAccompaniedAlternative(condition);
        this.reasons.Add(new AttractionCompatibilityReason(code, condition));
    }

    public void AddUnknown(
        AttractionCompatibilityReasonCode code,
        AttractionAccessCondition condition,
        IReadOnlyCollection<AttractionAccessConditionEvidenceIssue> evidenceIssues,
        IReadOnlyCollection<AttractionAccessConditionSemanticIssue> semanticIssues)
    {
        this.HasUnknown = true;
        this.MarkUnresolvedAccompaniedAlternative(condition);
        this.reasons.Add(new AttractionCompatibilityReason(
            code,
            condition,
            evidenceIssues,
            semanticIssues));
    }

    public void AddUnknown(AttractionCompatibilityReasonCode code)
    {
        this.HasUnknown = true;
        this.reasons.Add(new AttractionCompatibilityReason(code));
    }

    public bool TryAddUnresolvedAccompaniedAlternative(
        AttractionCompatibilityReasonCode code,
        AttractionAccessCondition condition,
        IReadOnlyCollection<AttractionAccessConditionEvidenceIssue>? evidenceIssues = null,
        IReadOnlyCollection<AttractionAccessConditionSemanticIssue>? semanticIssues = null)
    {
        if (!IsAccompaniedAlternative(condition))
        {
            return false;
        }

        this.MarkUnresolvedAccompaniedAlternative(condition);
        this.reasons.Add(new AttractionCompatibilityReason(
            code,
            condition,
            evidenceIssues,
            semanticIssues));
        return true;
    }

    public void ActivateUnresolvedAccompaniedAlternative()
    {
        this.HasUnknown = true;
    }

    public void AddConflictingConditions(
        IEnumerable<AttractionAccessCondition> conditions)
    {
        this.HasUnknown = true;
        this.AddConflictingConditionReasons(conditions);
    }

    public void AddUnresolvedAccompaniedConflictingConditions(
        IReadOnlyCollection<AttractionAccessCondition> conditions)
    {
        foreach (AttractionAccessCondition condition in conditions)
        {
            this.MarkUnresolvedAccompaniedAlternative(condition);
        }

        this.AddConflictingConditionReasons(conditions);
    }

    public void AddCompanionRequirementMet(AttractionAccessCondition condition)
    {
        this.RequiresCompanion = true;
        this.reasons.Add(new AttractionCompatibilityReason(
            AttractionCompatibilityReasonCode.AccompanimentRequirementMet,
            condition));
    }

    private void MarkUnresolvedAccompaniedAlternative(AttractionAccessCondition condition)
    {
        bool requiresAccompaniment = condition.RequiresAccompaniment == true;
        if (condition.Type == AttractionAccessConditionType.MinHeightAccompanied
            || (condition.Type == AttractionAccessConditionType.MinHeight
                && requiresAccompaniment))
        {
            this.HasUnresolvedHeightAccompaniedAlternative = true;
        }

        if (condition.Type == AttractionAccessConditionType.MinAgeAccompanied
            || (condition.Type == AttractionAccessConditionType.MinAge
                && requiresAccompaniment))
        {
            this.HasUnresolvedAgeAccompaniedAlternative = true;
        }
    }

    private static bool IsAccompaniedAlternative(AttractionAccessCondition condition)
    {
        return condition.Type is AttractionAccessConditionType.MinHeightAccompanied
                or AttractionAccessConditionType.MinAgeAccompanied
            || (condition.Type is AttractionAccessConditionType.MinHeight
                    or AttractionAccessConditionType.MinAge
                && condition.RequiresAccompaniment == true);
    }

    private void AddConflictingConditionReasons(
        IEnumerable<AttractionAccessCondition> conditions)
    {
        foreach (AttractionAccessCondition condition in conditions)
        {
            this.reasons.Add(new AttractionCompatibilityReason(
                AttractionCompatibilityReasonCode.ConflictingConditions,
                condition));
        }
    }
}
