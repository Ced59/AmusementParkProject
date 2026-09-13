namespace AmusementPark.WebAPI.Contracts.ParkDataEditorOperations;

public sealed class ParkDataEditorActiveRequestDto
{
    public string OperationId { get; set; } = string.Empty;

    public string Kind { get; set; } = string.Empty;

    public string Method { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public DateTime StartedAtUtc { get; set; }

    public bool InitiatedByCurrentToken { get; set; }
}
