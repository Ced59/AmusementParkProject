namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Résultat agrégé des contraintes éliminatoires évaluées avant le score.
/// </summary>
public enum ParkFitHardFilterState
{
    Passed,
    Failed,
    Unknown,
}
