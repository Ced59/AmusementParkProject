using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.ParkFit;

namespace AmusementPark.Application.Features.ParkFit.Commands;

public sealed record ReviewParkFitSourceReportCommand(
    string ReportId,
    ParkFitSourceReportStatus Decision,
    string ReviewerUserId,
    string? DecisionNote,
    long ExpectedRevision) : ICommand<ApplicationResult>;
