using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

/// <summary>
/// Unité HTTP de contrainte d'accès d'une attraction.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttractionAccessConditionUnitDto
{
    Centimeter,
    Year,
}
