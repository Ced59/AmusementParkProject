using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Parks;

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
