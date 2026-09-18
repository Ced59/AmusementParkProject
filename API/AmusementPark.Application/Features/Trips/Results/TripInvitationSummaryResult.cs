using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripInvitationSummaryResult(
    string InvitationId,
    string TokenHint,
    TripDelegatedRole ProposedRole,
    DateTime ExpiresAtUtc,
    bool IsTargeted,
    long Version,
    DateTime CreatedAtUtc);
