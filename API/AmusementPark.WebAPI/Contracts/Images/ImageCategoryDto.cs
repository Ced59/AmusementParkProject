using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Images;

/// <summary>
/// Catégorie HTTP d'image.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImageCategoryDto
{
    AVATAR = 0,
    PARK_LOGO = 1,
    PARK = 2,
    PARK_ITEM = 3,
    OPERATOR = 4,
    MANUFACTURER = 5,
    FOUNDER = 6,
    VIDEO_THUMBNAIL = 7,
}
