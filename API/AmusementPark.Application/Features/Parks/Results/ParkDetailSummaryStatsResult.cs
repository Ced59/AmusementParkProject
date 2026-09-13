using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.Ratings.Results;

namespace AmusementPark.Application.Features.Parks.Results;

/// <summary>
/// Compteurs agrégés nécessaires à ParkDetailLight sans charger les listes complètes.
/// </summary>
public sealed class ParkDetailSummaryStatsResult
{
    public int TotalItems { get; init; }

    public int ZoneCount { get; init; }

    public int MappableItemsCount { get; init; }

    public int OfficialMapsCount { get; init; }

    public int AttractionCount { get; init; }

    public int RestaurantCount { get; init; }

    public int ShowCount { get; init; }

    public int ShopCount { get; init; }

    public int HotelCount { get; init; }

    public IReadOnlyDictionary<ParkItemCategory, int> CountsByCategory { get; init; } = new Dictionary<ParkItemCategory, int>();
}
