namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Confiance agrégée du moteur, distincte de la confiance d'une source unique.
/// </summary>
public enum ParkFitDataConfidence
{
    Unknown,
    Low,
    Medium,
    High,
}
