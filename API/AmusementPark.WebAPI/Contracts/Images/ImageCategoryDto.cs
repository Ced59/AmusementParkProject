using System.Text.Json.Serialization;

namespace AmusementPark.WebAPI.Contracts.Images;

/// <summary>
/// Catégorie HTTP d'image.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ImageCategoryDto
{
    AVATAR,
    PARK_LOGO,
    PARK,
    ATTRACTION,
}
