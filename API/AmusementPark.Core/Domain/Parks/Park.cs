using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Agrégat métier représentant un parc.
/// </summary>
public sealed class Park : GeolocatedEntityBase
{
    /// <summary>
    /// Nom principal du parc.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Code pays ISO alpha-2.
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Type de parc.
    /// </summary>
    public ParkType? Type { get; set; }

    /// <summary>
    /// Identifiant du fondateur associé.
    /// </summary>
    public string? FounderId { get; set; }

    /// <summary>
    /// Identifiant de l'exploitant associé.
    /// </summary>
    public string? OperatorId { get; set; }

    /// <summary>
    /// Descriptions localisées.
    /// </summary>
    public List<LocalizedText> Descriptions { get; set; } = new();

    /// <summary>
    /// Indique si le parc est visible publiquement.
    /// </summary>
    public bool IsVisible { get; set; }

    /// <summary>
    /// URL du site officiel.
    /// </summary>
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Rue de l'adresse postale.
    /// </summary>
    public string? Street { get; set; }

    /// <summary>
    /// Ville de l'adresse postale.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Code postal de l'adresse postale.
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Identifiant de l'image de logo courante.
    /// </summary>
    public string? CurrentLogoImageId { get; set; }
}
