using AmusementPark.Core.Geo;

namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Points d'accès fonctionnels d'une attraction.
/// </summary>
public sealed class AttractionLocations
{
    /// <summary>
    /// Position de l'entrée standard.
    /// </summary>
    public GeoPoint? Entrance { get; set; }

    /// <summary>
    /// Position de la sortie.
    /// </summary>
    public GeoPoint? Exit { get; set; }

    /// <summary>
    /// Position de l'entrée coupe-file.
    /// </summary>
    public GeoPoint? FastPassEntrance { get; set; }

    /// <summary>
    /// Position de l'entrée mobilité réduite.
    /// </summary>
    public GeoPoint? ReducedMobilityEntrance { get; set; }
}
