namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Détermine si une condition d'accès concerne une date donnée.
/// </summary>
public static class AttractionAccessConditionApplicabilityEvaluator
{
    public static bool IsApplicableOn(
        AttractionAccessCondition condition,
        DateOnly evaluationDate)
    {
        ArgumentNullException.ThrowIfNull(condition);

        if (condition.EffectiveFrom.HasValue
            && condition.EffectiveTo.HasValue
            && condition.EffectiveTo.Value < condition.EffectiveFrom.Value)
        {
            return true;
        }

        return (!condition.EffectiveFrom.HasValue || condition.EffectiveFrom.Value <= evaluationDate)
            && (!condition.EffectiveTo.HasValue || condition.EffectiveTo.Value >= evaluationDate);
    }
}
