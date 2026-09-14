using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Évalue une exigence d'accompagnement sans supposer l'âge de l'accompagnant.
/// </summary>
internal static class AttractionAccompanimentEvaluator
{
    public static void Evaluate(
        ParkFitMemberProfile profile,
        IReadOnlyCollection<AttractionAccessCondition> conditions,
        AttractionCompatibilityEvaluationContext context,
        AttractionAccessCondition? uncertainAloneThreshold = null,
        bool hasUnresolvedAloneAlternative = false)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(context);

        AttractionAccessCondition? representative = conditions
            .OrderByDescending(static condition => condition.MinimumCompanionAge ?? 0)
            .ThenBy(static condition => condition.Type)
            .ThenByDescending(static condition => condition.Value)
            .ThenBy(static condition => condition.Unit)
            .ThenBy(static condition => condition.Scope)
            .ThenBy(static condition => condition.ScopeDetail, StringComparer.Ordinal)
            .ThenBy(static condition => condition.SourceKind)
            .ThenBy(static condition => condition.SourceUrl, StringComparer.Ordinal)
            .ThenBy(static condition => condition.SourceReference, StringComparer.Ordinal)
            .ThenBy(static condition => condition.SourceLanguageCode, StringComparer.Ordinal)
            .ThenBy(static condition => condition.EffectiveFrom)
            .ThenBy(static condition => condition.EffectiveTo)
            .FirstOrDefault();
        if (representative is null)
        {
            return;
        }

        if (!profile.CanBeAccompanied.HasValue)
        {
            context.AddUnknown(
                AttractionCompatibilityReasonCode.AccompanimentAvailabilityMissing,
                representative);
            return;
        }

        if (!profile.CanBeAccompanied.Value)
        {
            AddUnavailableResult(
                AttractionCompatibilityReasonCode.AccompanimentUnavailable,
                representative,
                uncertainAloneThreshold,
                hasUnresolvedAloneAlternative,
                context);
            return;
        }

        int requiredCompanionAge = conditions.Max(static condition => condition.MinimumCompanionAge ?? 0);
        if (requiredCompanionAge == 0)
        {
            context.AddCompanionRequirementMet(representative);
            return;
        }

        ParkFitAgeRange? companionAgeRange = profile.AvailableCompanionAgeRange;
        if (companionAgeRange is null)
        {
            context.AddUnknown(
                AttractionCompatibilityReasonCode.CompanionAgeRangeMissing,
                representative);
            return;
        }

        if (companionAgeRange.MaximumYears < requiredCompanionAge)
        {
            AddUnavailableResult(
                AttractionCompatibilityReasonCode.CompanionTooYoung,
                representative,
                uncertainAloneThreshold,
                hasUnresolvedAloneAlternative,
                context);
            return;
        }

        if (companionAgeRange.MinimumYears < requiredCompanionAge)
        {
            context.AddUnknown(
                AttractionCompatibilityReasonCode.CompanionAgeRangeCrossesThreshold,
                representative);
            return;
        }

        context.AddCompanionRequirementMet(representative);
    }

    private static void AddUnavailableResult(
        AttractionCompatibilityReasonCode reasonCode,
        AttractionAccessCondition accompaniedCondition,
        AttractionAccessCondition? uncertainAloneThreshold,
        bool hasUnresolvedAloneAlternative,
        AttractionCompatibilityEvaluationContext context)
    {
        if (uncertainAloneThreshold is null && !hasUnresolvedAloneAlternative)
        {
            context.AddViolation(reasonCode, accompaniedCondition);
            return;
        }

        context.AddInformational(reasonCode, accompaniedCondition);
        if (uncertainAloneThreshold is not null)
        {
            context.AddUnknown(
                AttractionCompatibilityReasonCode.AgeRangeCrossesThreshold,
                uncertainAloneThreshold);
        }
        else
        {
            context.ActivateUnresolvedAlternative();
        }
    }
}
