using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.AttractionManufacturers;

/// <summary>
/// Contrat HTTP retourné pour un constructeur d'attractions.
/// </summary>
public sealed class AttractionManufacturerDto
{
    /// <summary>
    /// Identifiant du constructeur.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Nom du constructeur.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Biographie localisée.
    /// </summary>
    public List<LocalizedTextDto> Biography { get; set; } = new();

    /// <summary>
    /// Nombre d'attractions rattachées.
    /// </summary>
    public int AttractionCount { get; set; }
}
