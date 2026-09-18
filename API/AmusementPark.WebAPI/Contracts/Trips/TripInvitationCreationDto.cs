using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed record TripInvitationCreationDto(
    string InvitationId,
    string Token,
    TripDelegatedRole ProposedRole,
    DateTime ExpiresAtUtc,
    bool IsTargeted,
    bool WasReplayed);
