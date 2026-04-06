namespace AmusementPark.WebAPI.Contracts.ParkItems;

/// <summary>
/// Points fonctionnels HTTP d'une attraction.
/// </summary>
public sealed class AttractionLocationsDto
{
    public AttractionLocationPointDto? Entrance { get; set; }

    public AttractionLocationPointDto? Exit { get; set; }

    public AttractionLocationPointDto? FastPassEntrance { get; set; }

    public AttractionLocationPointDto? ReducedMobilityEntrance { get; set; }
}
