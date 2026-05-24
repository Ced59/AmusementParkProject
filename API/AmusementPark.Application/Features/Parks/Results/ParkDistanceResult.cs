using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Results;

/// <summary>
/// Résultat applicatif d'un calcul de distance entre parcs.
/// </summary>
public sealed record ParkDistanceResult(
    Park SourcePark,
    IReadOnlyCollection<ParkDistanceTargetResult> Targets,
    IReadOnlyCollection<string> MissingTargetParkIds,
    IReadOnlyCollection<string> UnavailableTargetParkIds,
    string DistanceUnit,
    string CalculationKind);

/// <summary>
/// Résultat applicatif de distance vers un parc cible.
/// </summary>
public sealed record ParkDistanceTargetResult(
    Park Park,
    double DistanceKilometers,
    int EstimatedTravelDurationMinutes,
    int ProximityRank);
