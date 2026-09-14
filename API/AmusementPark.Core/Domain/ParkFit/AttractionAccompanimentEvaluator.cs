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
        AttractionCompatibilityEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(conditions);
        ArgumentNullException.ThrowIfNull(context);

        AttractionAccessCondition? representative = conditions
            .OrderByDescending(static condition => condition.MinimumCompanionAge ?? 0)
            .ThenBy(static condition => condition.Type)
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
            context.AddViolation(
                AttractionCompatibilityReasonCode.AccompanimentUnavailable,
                representative);
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
            context.AddViolation(
                AttractionCompatibilityReasonCode.CompanionTooYoung,
                representative);
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
}
