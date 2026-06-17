namespace AmusementPark.Core.Domain.Videos;

public enum VideoOwnerType
{
    None = 0,
    Park = 1,
    ParkItem = 2,
    [Obsolete("Use ParkItem.")]
    Attraction = ParkItem,
}
