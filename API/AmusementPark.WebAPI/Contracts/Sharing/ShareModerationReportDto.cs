namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class ShareModerationReportDto
{
    public string ReportId { get; init; } = string.Empty;

    public string TargetType { get; init; } = string.Empty;

    public string Reason { get; init; } = string.Empty;

    public string? Details { get; init; }

    public string Status { get; init; } = string.Empty;

    public DateTime SubmittedAtUtc { get; init; }

    public DateTime? ReviewedAtUtc { get; init; }

    public string? DecisionNote { get; init; }
}
