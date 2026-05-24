using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Images;

/// <summary>
/// Type de propriétaire HTTP d'image.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImageOwnerTypeDto
{
    NONE = 0,
    PARK = 1,
    USER = 2,
    ATTRACTION = 3,
    PARK_OPERATOR = 4,
    ATTRACTION_MANUFACTURER = 5,
    PARK_FOUNDER = 6,
}
