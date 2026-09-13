namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Composants effectivement retenus par la politique pour calculer la note d'un parc.
/// </summary>
public enum ParkRankingCompositionMode
{
    None = 0,
    DirectOnly = 1,
    ItemsOnly = 2,
    DirectAndItems = 3,
}
