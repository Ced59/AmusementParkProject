using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.Contracts.Ratings;

namespace AmusementPark.WebAPI.Contracts.Parks;

public sealed class ParkDetailSummaryStatsDto
{
    public int TotalItems { get; set; }

    public int ZoneCount { get; set; }

    public int MappableItemsCount { get; set; }

    public int OfficialMapsCount { get; set; }

    public int AttractionCount { get; set; }

    public int RestaurantCount { get; set; }

    public int ShowCount { get; set; }

    public int ShopCount { get; set; }

    public int HotelCount { get; set; }

    public Dictionary<string, int> CountsByCategory { get; set; } = new Dictionary<string, int>();
}
