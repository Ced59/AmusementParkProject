using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Common;

/// <summary>
/// Contrat HTTP d'action de masse sur une liste d'administration.
/// </summary>
public sealed class BulkAdministrationUpdateDto
{
    public List<string> Ids { get; set; } = new();

    public bool? IsVisible { get; set; }

    public AdminReviewStatusDto? AdminReviewStatus { get; set; }
}

/// <summary>
/// Résultat HTTP d'action de masse sur une liste d'administration.
/// </summary>
public sealed class BulkAdministrationUpdateResultDto
{
    public int RequestedCount { get; set; }

    public int UpdatedCount { get; set; }
}
