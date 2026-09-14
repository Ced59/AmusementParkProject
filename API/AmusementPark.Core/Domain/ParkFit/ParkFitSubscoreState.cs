namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Disponibilité d'une composante sans assimiler l'inconnu à zéro.
/// </summary>
public enum ParkFitSubscoreState
{
    Known,
    Unknown,
    NotApplicable,
}
