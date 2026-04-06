namespace AmusementPark.Application.Features.CaptainCoaster.Contracts;

/// <summary>
/// Décrit un choix humain de résolution de doublon Captain Coaster.
/// </summary>
public sealed class CaptainCoasterDuplicateResolution
{
    public string DuplicateGroupId { get; init; } = string.Empty;

    public IReadOnlyCollection<string> SelectedSnapshotIds { get; init; } = Array.Empty<string>();

    public bool MergeSelectedEntries { get; init; }
}
