namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Résultat déclaré pour une occurrence dans l'historique d'une visite.
/// </summary>
public enum RideOccurrenceStatus
{
    Completed = 1,
    Attempted = 2,
    MissedClosed = 3,
    MissedUnavailable = 4,
    SkippedByChoice = 5,
}
