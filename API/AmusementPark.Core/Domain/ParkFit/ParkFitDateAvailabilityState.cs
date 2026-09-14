namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Disponibilité factuelle du parc à la date demandée.
/// </summary>
public enum ParkFitDateAvailabilityState
{
    NotRequested,
    Available,
    Unavailable,
    Unknown,
}
