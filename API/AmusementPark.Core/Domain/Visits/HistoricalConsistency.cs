namespace AmusementPark.Core.Domain.Visits;

/// <summary>
/// Niveau de cohérence connu entre la cible et la date historique déclarée.
/// </summary>
public enum HistoricalConsistency
{
    Verified = 1,
    Unverified = 2,
    ConfirmedConflict = 3,
}
