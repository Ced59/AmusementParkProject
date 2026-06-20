namespace AmusementPark.Application.Features.Countries;

/// <summary>
/// Région géographique publique utilisée pour filtrer les parcs visibles.
/// </summary>
public enum WorldRegionFilter
{
    /// <summary>
    /// Europe géographique et territoires européens proches.
    /// </summary>
    Europe,

    /// <summary>
    /// Amérique du Nord, Amérique centrale et Caraïbes.
    /// </summary>
    NorthAmerica,

    /// <summary>
    /// Amérique du Sud.
    /// </summary>
    SouthAmerica,

    /// <summary>
    /// Asie.
    /// </summary>
    Asia,

    /// <summary>
    /// Moyen-Orient.
    /// </summary>
    MiddleEast,

    /// <summary>
    /// Océanie.
    /// </summary>
    Oceania,

    /// <summary>
    /// Ancien regroupement Asie, Moyen-Orient et Océanie conservé pour les anciens liens.
    /// </summary>
    Orient,

    /// <summary>
    /// Afrique.
    /// </summary>
    Africa
}
