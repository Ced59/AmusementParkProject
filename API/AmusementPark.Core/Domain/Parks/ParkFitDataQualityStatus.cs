namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Niveau d'utilisation autorisé pour les données d'un parc dans le moteur FIT.
/// </summary>
public enum ParkFitDataQualityStatus
{
    NotAssessed,
    Insufficient,
    EligibleForDiscoveryOnly,
    EligibleForFitComparison,
    TemporarilyStale,
    Suspended,
}
