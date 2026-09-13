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

    public bool HasCurrentPricing { get; set; }

    public ParkDetailSummaryStatsDto Stats { get; set; } = new ParkDetailSummaryStatsDto();
}
