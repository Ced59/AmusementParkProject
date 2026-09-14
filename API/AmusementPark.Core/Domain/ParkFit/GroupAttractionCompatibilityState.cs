namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Verdict prudent pour un groupe et une attraction.
/// </summary>
public enum GroupAttractionCompatibilityState
{
    EveryoneTogether,
    PossibleWithSplit,
    Partial,
    None,
    Unknown,
}
