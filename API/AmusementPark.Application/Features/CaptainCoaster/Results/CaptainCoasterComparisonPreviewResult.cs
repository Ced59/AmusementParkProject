namespace AmusementPark.Application.Features.CaptainCoaster.Results;

/// <summary>
/// Prévisualisation applicative d'une comparaison Captain Coaster.
/// </summary>
public sealed class CaptainCoasterComparisonPreviewResult
{
    public int AddedCount { get; init; }

    public int UpdatedCount { get; init; }

    public int RemovedCount { get; init; }

    public int DuplicateCount { get; init; }
}
