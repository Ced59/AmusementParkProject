using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Commands;

public sealed record SubmitShareModerationReportCommand(
    ShareModerationTargetType TargetType,
    string ShareId,
    ShareModerationReason Reason,
    string? Details) : ICommand<ApplicationResult>;
