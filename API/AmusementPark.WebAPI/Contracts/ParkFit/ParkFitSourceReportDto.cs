namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class ParkFitSourceReportDto
{
    public string ReportId { get; init; } = string.Empty;
    public string ParkId { get; init; } = string.Empty;
    public string ParkName { get; init; } = string.Empty;
    public string EvidenceKind { get; init; } = string.Empty;
    public string? SourceUrl { get; init; }
    public string? SourceReference { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string? Details { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime SubmittedAtUtc { get; init; }
    public DateTime? ReviewedAtUtc { get; init; }
    public string? DecisionNote { get; init; }
    public long Revision { get; init; }
}
