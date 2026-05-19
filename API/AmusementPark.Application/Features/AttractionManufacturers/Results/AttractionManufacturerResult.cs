using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.AttractionManufacturers.Results;

/// <summary>
/// Résultat applicatif de lecture/écriture d'un constructeur d'attractions.
/// </summary>
public sealed class AttractionManufacturerResult
{
    /// <summary>
    /// Identifiant du constructeur.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Nom du constructeur.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Biographie localisée.
    /// </summary>
    public IReadOnlyCollection<LocalizedText> Biography { get; init; } = Array.Empty<LocalizedText>();

    /// <summary>
    /// Nombre d'attractions liées.
    /// </summary>
    public int AttractionCount { get; init; }

    /// <summary>
    /// Statut de revue interne back-office.
    /// </summary>
    public AdminReviewStatus AdminReviewStatus { get; init; }
}

