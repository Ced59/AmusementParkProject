namespace AmusementPark.WebAPI.Contracts.History;

public sealed class HistoricalEditorialMutationDto
{
    public string ResourceType { get; init; } = string.Empty;

    public string ResourceId { get; init; } = string.Empty;

    public int Revision { get; init; }

    public string WorkflowState { get; init; } = string.Empty;

    public string PublicationState { get; init; } = string.Empty;

    public string? FactState { get; init; }
}
