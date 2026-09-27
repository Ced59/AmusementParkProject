using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.HistoricalExistenceReports.Results;

public sealed record HistoricalExistenceReportResult(
    string ReportId,
    string ParkId,
    string ParkName,
    VisitDate VisitDate,
    string ClaimedName,
    string? SourceUrl,
    string? SourceReference,
    string? Details,
    HistoricalExistenceReportStatus Status,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc,
    string? DecisionNote,
    long Revision);
