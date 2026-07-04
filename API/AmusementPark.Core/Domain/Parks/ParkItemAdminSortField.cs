namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Champs autorisés pour le tri des listes d'administration des éléments de parc.
/// </summary>
public enum ParkItemAdminSortField
{
    Default = 0,
    Name = 1,
    Category = 2,
    Type = 3,
    IsVisible = 4,
    AdminReviewStatus = 5,
    ParkId = 6,
    ZoneId = 7,
    DataCompletenessScore = 8,
}
