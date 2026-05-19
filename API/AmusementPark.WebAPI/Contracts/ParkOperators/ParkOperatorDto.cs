using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkOperators;

/// <summary>
/// Contrat HTTP retourné pour un exploitant de parc.
/// </summary>
public sealed class ParkOperatorDto
{
    /// <summary>
    /// Identifiant de l'exploitant.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Nom de l'exploitant.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description localisée.
    /// </summary>
    public List<LocalizedTextDto> Description { get; set; } = new();

    /// <summary>
    /// Statut de revue interne back-office.
    /// </summary>
    public AdminReviewStatusDto AdminReviewStatus { get; set; } = AdminReviewStatusDto.Validated;
}
