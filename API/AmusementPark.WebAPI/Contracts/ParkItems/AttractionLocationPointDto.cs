namespace AmusementPark.WebAPI.Contracts.ParkItems;

/// <summary>
/// Point géographique HTTP lié à une attraction.
/// </summary>
public sealed class AttractionLocationPointDto
{
    public double? Latitude { get; set; }

    public double? Longitude { get; set; }
}
