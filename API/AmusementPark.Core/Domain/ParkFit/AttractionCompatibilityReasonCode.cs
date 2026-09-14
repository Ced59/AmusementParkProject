namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Codes stables traduisibles qui expliquent un verdict individuel.
/// </summary>
public enum AttractionCompatibilityReasonCode
{
    NoApplicableCondition,
    ConditionDefinitionUnusable,
    ConditionEvidenceUnusable,
    ConflictingConditions,
    ScopedConditionRequiresConfiguration,
    HeightMissing,
    BelowMinimumHeight,
    AboveMaximumHeight,
    HeightRequirementMet,
    AgeRangeMissing,
    BelowMinimumAge,
    AgeRequirementMet,
    AgeRangeCrossesThreshold,
    AccompanimentAvailabilityMissing,
    AccompanimentUnavailable,
    CompanionAgeRangeMissing,
    CompanionTooYoung,
    CompanionAgeRangeCrossesThreshold,
    AccompanimentRequirementMet,
    PersonalRestrictionRequiresConfirmation,
}
