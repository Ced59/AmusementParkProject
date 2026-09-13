using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Results;

/// <summary>
/// Résultat applicatif de distance vers un parc cible.
/// </summary>
public sealed record ParkDistanceTargetResult(
    Park Park,
    double DistanceKilometers,
    int EstimatedTravelDurationMinutes,
    int ProximityRank);
