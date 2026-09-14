namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Verdict prudent pour une personne et une attraction.
/// </summary>
public enum AttractionCompatibilityState
{
    CompatibleAlone,
    CompatibleWithCompanion,
    Incompatible,
    Unknown,
    NotApplicable,
}
