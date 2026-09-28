namespace AmusementPark.WebAPI.Contracts.History;

public sealed class AdminHistoricalParkDiagnosticsDto
{
    public string ParkId { get; init; } = string.Empty;

    public string ParkName { get; init; } = string.Empty;

    public int FactCount { get; init; }

    public int RelationCount { get; init; }

    public int BlockingIssueCount { get; init; }

    public IReadOnlyCollection<AdminHistoricalDiagnosticIssueDto> Issues { get; init; } =
        Array.Empty<AdminHistoricalDiagnosticIssueDto>();

    public IReadOnlyCollection<AdminHistoricalDecadeCoverageDto> DecadeCoverage { get; init; } =
        Array.Empty<AdminHistoricalDecadeCoverageDto>();

    public IReadOnlyCollection<AdminHistoricalWorkflowStageDto> Workflow { get; init; } =
        Array.Empty<AdminHistoricalWorkflowStageDto>();

    public AdminHistoricalVisitDiagnosticsDto Visits { get; init; } = new();

    public AdminHistoricalParkRolloutGateDto RolloutGate { get; init; } = new();
}
