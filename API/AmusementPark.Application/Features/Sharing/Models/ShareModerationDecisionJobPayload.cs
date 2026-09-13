using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Models;

public sealed record ShareModerationDecisionJobPayload(
    string ReportId,
    ShareModerationDecision Decision,
    string ReviewerUserId,
    string? Note,
    DateTime RequestedAtUtc,
    long ReportVersion = 0,
    int Continuation = 0);
