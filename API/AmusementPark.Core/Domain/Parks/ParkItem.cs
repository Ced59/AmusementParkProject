using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Élément fonctionnel d'un parc.
/// </summary>
public sealed class ParkItem : GeolocatedEntityBase
{
    /// <summary>
    /// Identifiant du parc parent.
    /// </summary>
    public string ParkId { get; set; } = string.Empty;

    /// <summary>
    /// Identifiant éventuel de la zone parent.
    /// </summary>
    public string? ZoneId { get; set; }

    /// <summary>
    /// Nom d'affichage.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Catégorie métier globale.
    /// </summary>
    public ParkItemCategory Category { get; set; }

    /// <summary>
    /// Type métier détaillé.
    /// </summary>
    public ParkItemType Type { get; set; }

    /// <summary>
    /// Sous-type libre éventuel.
    /// </summary>
    public string? Subtype { get; set; }

    /// <summary>
    /// Descriptions localisées.
    /// </summary>
    public List<LocalizedText> Descriptions { get; set; } = new();

    /// <summary>
    /// Détails spécifiques aux attractions.
    /// </summary>
    public AttractionDetails? AttractionDetails { get; set; }

    /// <summary>
    /// Localisations fonctionnelles spécifiques à l'attraction.
    /// </summary>
    public AttractionLocations? AttractionLocations { get; set; }

    /// <summary>
    /// Indique si l'élément est visible publiquement.
    /// </summary>
    public bool IsVisible { get; set; } = true;
}
