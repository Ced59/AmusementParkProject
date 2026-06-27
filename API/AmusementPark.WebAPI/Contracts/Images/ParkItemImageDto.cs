using AmusementPark.WebAPI.Contracts.ParkItems;

namespace AmusementPark.WebAPI.Contracts.Images;

public sealed class ParkItemImageDto
{
    public ImageDto Image { get; set; } = new();

    public ParkItemDto Item { get; set; } = new();
}
