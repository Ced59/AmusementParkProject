using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Zone fonctionnelle d'un parc.
/// </summary>
public sealed class ParkZone : GeolocatedEntityBase
{
    /// <summary>
    /// Identifiant du parc parent.
    /// </summary>
    public string ParkId { get; set; } = string.Empty;

    /// <summary>
    /// Nom hérité legacy conservé pour compatibilité fonctionnelle.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Noms localisés de la zone.
    /// </summary>
    public List<LocalizedText> Names { get; set; } = new();

    /// <summary>
    /// Slug de navigation éventuel.
    /// </summary>
    public string? Slug { get; set; }

    /// <summary>
    /// Descriptions localisées.
    /// </summary>
    public List<LocalizedText> Descriptions { get; set; } = new();

    /// <summary>
    /// Indique si la zone est visible publiquement.
    /// </summary>
    public bool IsVisible { get; set; } = true;

    /// <summary>
    /// Ordre d'affichage.
    /// </summary>
    public int SortOrder { get; set; }
}
