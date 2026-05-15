using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

/// <summary>
/// Catégorie HTTP d'un park item.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ParkItemCategoryDto
{
    Attraction,
    Restaurant,
    Hotel,
    Animal,
    Show,
    Shop,
    Service,
    Transport,
    Other,
}
