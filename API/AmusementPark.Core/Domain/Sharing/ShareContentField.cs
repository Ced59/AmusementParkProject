namespace AmusementPark.Core.Domain.Sharing;

/// <summary>
/// Champs publics autorisables en V1. Les données privées interdites sont absentes de cette liste blanche.
/// </summary>
public enum ShareContentField
{
    PublicDisplayName = 1,
    Avatar = 2,
    RideCount = 3,
    TemporalRatings = 4,
    GlobalRatings = 5,
    PublicCaption = 6,
    GeographicStatistics = 7,
    MissedItems = 8,
}
