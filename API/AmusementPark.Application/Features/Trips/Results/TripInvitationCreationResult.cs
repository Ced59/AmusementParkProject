using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripInvitationCreationResult(
    string InvitationId,
    string Token,
    string InviterDisplayName,
    TripDelegatedRole ProposedRole,
    DateTime ExpiresAtUtc,
    bool IsTargeted,
    bool WasReplayed);
