using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.HistoricalExistenceReports.Commands;

public sealed record ReviewHistoricalExistenceReportCommand(
    string ReportId,
    HistoricalExistenceReportStatus Decision,
    string ReviewerUserId,
    string? DecisionNote,
    long ExpectedRevision) : ICommand<ApplicationResult>;
