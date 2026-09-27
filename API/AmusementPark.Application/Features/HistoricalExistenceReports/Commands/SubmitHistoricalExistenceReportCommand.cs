using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;

namespace AmusementPark.Application.Features.HistoricalExistenceReports.Commands;

public sealed record SubmitHistoricalExistenceReportCommand(
    string UserId,
    string VisitId,
    string ClaimedName,
    string? SourceUrl,
    string? SourceReference,
    string? Details) : ICommand<ApplicationResult<Results.HistoricalExistenceReportResult>>;
