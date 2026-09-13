using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.Parks;

public sealed class ParkMapAttractionDetailsDto
{
    public string? ManufacturerId { get; set; }

    public string? Model { get; set; }

    public string? Status { get; set; }
}
