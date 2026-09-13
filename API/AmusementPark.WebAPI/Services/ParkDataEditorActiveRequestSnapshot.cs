namespace AmusementPark.WebAPI.Services;

public sealed class ParkDataEditorActiveRequestSnapshot
{
    public string OperationId { get; init; } = string.Empty;

    public ParkDataEditorOperationKind Kind { get; init; }

    public string Method { get; init; } = string.Empty;

    public string Path { get; init; } = string.Empty;

    public DateTime StartedAtUtc { get; init; }

    public bool InitiatedByCurrentClient { get; init; }
}
