namespace AmusementPark.Core.Domain.Watchlists;

/// <summary>
/// État factuel minimal de la cible, conservé sans supprimer l'intention privée.
/// </summary>
public enum CollectionTargetStatus
{
    Unknown = 1,
    Available = 2,
    TemporarilyClosed = 3,
    PermanentlyClosed = 4,
}
