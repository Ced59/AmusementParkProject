using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

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

    public ParkDetailSummaryStatsResult Stats { get; init; } = new ParkDetailSummaryStatsResult();
}

/// <summary>
/// Compteurs agrégés nécessaires à ParkDetailLight sans charger les listes complètes.
/// </summary>
public sealed class ParkDetailSummaryStatsResult
{
    public int TotalItems { get; init; }

    public int ZoneCount { get; init; }

    public int AttractionCount { get; init; }

    public int RestaurantCount { get; init; }

    public int ShowCount { get; init; }

    public int ShopCount { get; init; }

    public int HotelCount { get; init; }

    public IReadOnlyDictionary<ParkItemCategory, int> CountsByCategory { get; init; } = new Dictionary<ParkItemCategory, int>();
}
