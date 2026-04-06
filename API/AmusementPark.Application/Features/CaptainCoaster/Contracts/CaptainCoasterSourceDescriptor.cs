namespace AmusementPark.Application.Features.CaptainCoaster.Contracts;

/// <summary>
/// Décrit une source d'import Captain Coaster.
/// </summary>
public sealed class CaptainCoasterSourceDescriptor
{
    public string SourceKind { get; init; } = string.Empty;

    public string PathOrIdentifier { get; init; } = string.Empty;

    public bool IsOfflineSource { get; init; }
}
