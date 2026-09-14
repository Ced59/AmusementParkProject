using AmusementPark.Core.Domain.ParkFit;

namespace AmusementPark.Application.Features.ParkFit.Results;

public sealed record ParkFitSourceReportResult(
    string ReportId,
    string ParkId,
    string ParkName,
    ParkFitEvidenceKind EvidenceKind,
    string? SourceUrl,
    string? SourceReference,
    ParkFitSourceReportReason Reason,
    string? Details,
    ParkFitSourceReportStatus Status,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc,
    string? DecisionNote,
    long Revision);
