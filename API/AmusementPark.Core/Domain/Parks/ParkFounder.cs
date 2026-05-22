using AmusementPark.Core.Abstractions;
using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Fondateur d'un parc.
/// </summary>
public sealed class ParkFounder : AuditableEntity
{
    /// <summary>
    /// Nom du fondateur.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Métier ou rôle principal.
    /// </summary>
    public string? Occupation { get; set; }

    /// <summary>
    /// Date de naissance, au format libre ou ISO selon la donnée disponible.
    /// </summary>
    public string? BirthDate { get; set; }

    /// <summary>
    /// Date de décès éventuelle, au format libre ou ISO selon la donnée disponible.
    /// </summary>
    public string? DeathDate { get; set; }

    /// <summary>
    /// Lieu de naissance.
    /// </summary>
    public string? BirthPlace { get; set; }

    /// <summary>
    /// Nationalité ou pays de rattachement principal.
    /// </summary>
    public string? NationalityCountryCode { get; set; }

    /// <summary>
    /// Site web de référence.
    /// </summary>
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Biographie localisée.
    /// </summary>
    public List<LocalizedText> Biography { get; set; } = new();
}
