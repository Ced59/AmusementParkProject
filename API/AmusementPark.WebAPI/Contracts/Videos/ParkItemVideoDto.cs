using AmusementPark.WebAPI.Contracts.ParkItems;

namespace AmusementPark.WebAPI.Contracts.Videos;

public sealed class ParkItemVideoDto
{
    public required VideoDto Video { get; set; }

    public required ParkItemDto Item { get; set; }
}
