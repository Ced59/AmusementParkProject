namespace AmusementPark.WebAPI.Services;

internal sealed class ActiveOperationState
{
    public string OperationId { get; init; } = string.Empty;

    public string ClientId { get; init; } = string.Empty;

    public ParkDataEditorOperationKind Kind { get; init; }

    public string Method { get; init; } = string.Empty;

    public string Path { get; init; } = string.Empty;

    public DateTime StartedAtUtc { get; init; }
}
