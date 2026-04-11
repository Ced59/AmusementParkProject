using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkOperators;

/// <summary>
/// Contrat HTTP de création d'un exploitant de parc.
/// </summary>
public sealed class ParkOperatorCreateDto
{
    /// <summary>
    /// Nom de l'exploitant.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description localisée.
    /// </summary>
    public List<LocalizedTextDto> Description { get; set; } = new();
}
