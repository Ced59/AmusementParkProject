using AmusementPark.WebAPI.Contracts.ParkItems;

namespace AmusementPark.WebAPI.Contracts.Images;

public sealed class ParkItemImageDto
{
    public required ImageDto Image { get; set; }

    public required ParkItemDto Item { get; set; }
}
