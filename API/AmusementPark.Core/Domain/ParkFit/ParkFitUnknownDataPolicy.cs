namespace AmusementPark.Core.Domain.ParkFit;

/// <summary>
/// Choix explicite de traitement des inconnues critiques.
/// </summary>
public enum ParkFitUnknownDataPolicy
{
    KeepWithWarning,
    ExcludeUnknown,
    KnownOnly,
}
