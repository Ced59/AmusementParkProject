namespace AmusementPark.Core.Domain.Images;

/// <summary>
/// Type de propriétaire d'une image.
/// </summary>
public enum ImageOwnerType
{
    None = 0,
    Park = 1,
    User = 2,
    Attraction = 3,
    ParkOperator = 4,
    AttractionManufacturer = 5,
    ParkFounder = 6,
    Video = 7,
}
