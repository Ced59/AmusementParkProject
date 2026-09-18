namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed record TripInvitationCreationDto(
    string InvitationId,
    string Token,
    TripDelegatedRoleDto ProposedRole,
    DateTime ExpiresAtUtc,
    bool IsTargeted,
    bool WasReplayed);
