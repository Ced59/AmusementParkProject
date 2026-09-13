using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Commands;

public sealed record ReviewShareModerationReportCommand(
    string ReviewerUserId,
    string ReportId,
    ShareModerationDecision Decision,
    string? Note) : ICommand<ApplicationResult>;
