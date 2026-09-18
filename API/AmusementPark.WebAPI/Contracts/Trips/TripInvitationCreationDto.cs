namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed record TripInvitationCreationDto(
    string InvitationId,
    string Token,
    string InviterDisplayName,
    TripDelegatedRoleDto ProposedRole,
    DateTime ExpiresAtUtc,
    bool IsTargeted,
    bool WasReplayed);
