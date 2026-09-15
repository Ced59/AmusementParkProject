namespace AmusementPark.Core.Domain.Watchlists;

/// <summary>
/// Intention explicite d'un utilisateur envers un parc ou un élément de parc.
/// </summary>
public enum UserCollectionKind
{
    Favorite = 1,
    WantToVisit = 2,
    WantToExperience = 3,
    Planned = 4,
}
