using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkFounders;

/// <summary>
/// Contrat HTTP retourné pour un fondateur de parc.
/// </summary>
public sealed class ParkFounderDto
{
    /// <summary>
    /// Identifiant du fondateur.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Nom du fondateur.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Biographie localisée.
    /// </summary>
    public List<LocalizedTextDto> Biography { get; set; } = new();
}
