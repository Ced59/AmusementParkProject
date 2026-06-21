using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

public sealed class ParkItemsByParkVisibilityUpdateDto
{
    public List<string> ParkIds { get; set; } = new();

    public bool IsVisible { get; set; }
}
