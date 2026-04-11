using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.AttractionManufacturers;

/// <summary>
/// Contrat HTTP de mise à jour d'un constructeur d'attractions.
/// </summary>
public sealed class AttractionManufacturerUpdateDto
{
    /// <summary>
    /// Nom du constructeur.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Biographie localisée.
    /// </summary>
    public List<LocalizedTextDto> Biography { get; set; } = new();
}
