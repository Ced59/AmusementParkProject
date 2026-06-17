namespace AmusementPark.Core.Domain.Images;

/// <summary>
/// Catégorie fonctionnelle d'image.
/// </summary>
public enum ImageCategory
{
    Avatar = 0,
    ParkLogo = 1,
    Park = 2,
    ParkItem = 3,
    [Obsolete("Use ParkItem.")]
    Attraction = ParkItem,
    Operator = 4,
    Manufacturer = 5,
    Founder = 6,
    VideoThumbnail = 7,
}
