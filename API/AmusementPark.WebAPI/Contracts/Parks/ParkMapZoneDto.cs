using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Parks;

public sealed class ParkMapZoneDto
{
    public required string Id { get; set; }

    public required string Name { get; set; }

    public int SortOrder { get; set; }
}
