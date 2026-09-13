using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Common;

/// <summary>
/// Résultat HTTP d'action de masse sur une liste d'administration.
/// </summary>
public sealed class BulkAdministrationUpdateResultDto
{
    public int RequestedCount { get; set; }

    public int UpdatedCount { get; set; }
}
