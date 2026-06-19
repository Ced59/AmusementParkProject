using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Images;
using AmusementPark.WebAPI.Contracts.Ratings;

namespace AmusementPark.WebAPI.Contracts.Parks;

/// <summary>
/// Contrat HTTP léger pour la page publique ParkDetailLight.
/// </summary>
public sealed class ParkDetailSummaryDto
{
    public required ParkDto Park { get; set; }

    public ImageDto? MainImage { get; set; }

    public ParkDetailReferenceSummaryDto References { get; set; } = new ParkDetailReferenceSummaryDto();

    public RatingSummaryDto? Rating { get; set; }

    public ParkDetailSummaryStatsDto Stats { get; set; } = new ParkDetailSummaryStatsDto();
}

public sealed class ParkDetailReferenceSummaryDto
{
    public string? FounderName { get; set; }

    public string? OperatorName { get; set; }
}

public sealed class ParkDetailSummaryStatsDto
{
    public int TotalItems { get; set; }

    public int ZoneCount { get; set; }

    public int AttractionCount { get; set; }

    public int RestaurantCount { get; set; }

    public int ShowCount { get; set; }

    public int ShopCount { get; set; }

    public int HotelCount { get; set; }

    public Dictionary<string, int> CountsByCategory { get; set; } = new Dictionary<string, int>();
}
