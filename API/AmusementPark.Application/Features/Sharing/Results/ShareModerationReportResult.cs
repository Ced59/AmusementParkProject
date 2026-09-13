using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record ShareModerationReportResult(
    string ReportId,
    ShareModerationTargetType TargetType,
    ShareModerationReason Reason,
    string? Details,
    ShareModerationReportStatus Status,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc,
    string? DecisionNote);
