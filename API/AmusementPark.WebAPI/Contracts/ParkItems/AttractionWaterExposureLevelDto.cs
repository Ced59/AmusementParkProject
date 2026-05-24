using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

/// <summary>
/// Niveau d'exposition à l'eau HTTP d'une attraction.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttractionWaterExposureLevelDto
{
    None,
    Splash,
    Moderate,
    Soaking,
    ExtremeSoaking,
}
