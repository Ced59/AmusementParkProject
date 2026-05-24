namespace AmusementPark.Application.Common.Results;

/// <summary>
/// Résultat applicatif d'une action de masse d'administration.
/// </summary>
public sealed class BulkAdministrationUpdateResult
{
    public int RequestedCount { get; init; }

    public int UpdatedCount { get; init; }
}
