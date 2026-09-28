namespace AmusementPark.WebAPI.Contracts.History;

public sealed class AdminHistoricalWorkflowStageDto
{
    public string Stage { get; init; } = string.Empty;

    public int ResourceCount { get; init; }
}
