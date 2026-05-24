using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Results;

/// <summary>
/// Parc public mis en avant sur la home, enrichi par les compteurs nécessaires à la carte.
/// </summary>
public sealed record HomeFeaturedParkResult(
    Park Park,
    IReadOnlyDictionary<ParkItemCategory, int> CountsByCategory,
    bool IsManualFeatured);
