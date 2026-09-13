using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.Ratings.Results;

namespace AmusementPark.Application.Features.Parks.Results;

/// <summary>
/// Résumé public optimisé pour la page détail parc légère.
/// </summary>
public sealed class ParkDetailSummaryResult
{
    public required Park Park { get; init; }

    public Image? MainImage { get; init; }

    public string? FounderName { get; init; }

    public string? OperatorName { get; init; }

    public RatingSummaryResult? Rating { get; init; }

    public bool HasCurrentPricing { get; init; }

    public ParkDetailSummaryStatsResult Stats { get; init; } = new ParkDetailSummaryStatsResult();
}
