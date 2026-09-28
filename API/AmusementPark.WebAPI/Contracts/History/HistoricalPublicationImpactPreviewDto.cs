namespace AmusementPark.WebAPI.Contracts.History;

public sealed class HistoricalPublicationImpactPreviewDto
{
    public string ResourceType { get; init; } = string.Empty;

    public string ResourceId { get; init; } = string.Empty;

    public string ResourceLabel { get; init; } = string.Empty;

    public int PreviewYear { get; init; }

    public bool CanPublish { get; init; }

    public IReadOnlyCollection<string> BlockingReasons { get; init; } = Array.Empty<string>();

    public int? AffectedFromYear { get; init; }

    public int? AffectedToYear { get; init; }

    public int? AffectedSnapshotYearCount { get; init; }

    public int ChangedSubjectCount { get; init; }

    public HistoricalSnapshotImpactSummaryDto Before { get; init; } = new();

    public HistoricalSnapshotImpactSummaryDto After { get; init; } = new();

    public AdminHistoricalVisitDiagnosticsDto Visits { get; init; } = new();
}
