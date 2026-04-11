using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkFounders;

/// <summary>
/// Contrat HTTP de mise à jour d'un fondateur de parc.
/// </summary>
public sealed class ParkFounderUpdateDto
{
    /// <summary>
    /// Nom du fondateur.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Biographie localisée.
    /// </summary>
    public List<LocalizedTextDto> Biography { get; set; } = new();
}
