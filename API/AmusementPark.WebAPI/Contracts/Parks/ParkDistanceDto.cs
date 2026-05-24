using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Parks;

/// <summary>
/// Contrat HTTP d'un calcul de distance entre parcs.
/// </summary>
public sealed class ParkDistanceResponseDto
{
    public ParkMapPointDto Source { get; set; } = new();

    public string DistanceUnit { get; set; } = "km";

    public string CalculationKind { get; set; } = "great-circle";

    public List<ParkDistanceTargetDto> Targets { get; set; } = new();

    public List<string> MissingTargetParkIds { get; set; } = new();

    public List<string> UnavailableTargetParkIds { get; set; } = new();
}

/// <summary>
/// Contrat HTTP d'une distance vers un parc cible.
/// </summary>
public sealed class ParkDistanceTargetDto
{
    public int ProximityRank { get; set; }

    public double DistanceKilometers { get; set; }

    public double DistanceMeters { get; set; }

    public string DistanceUnit { get; set; } = "km";

    public int EstimatedTravelDurationMinutes { get; set; }

    public ParkDto Park { get; set; } = new();
}
