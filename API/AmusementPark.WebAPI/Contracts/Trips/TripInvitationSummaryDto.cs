namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed record TripInvitationSummaryDto(
    string InvitationId,
    string TokenHint,
    TripDelegatedRoleDto ProposedRole,
    DateTime ExpiresAtUtc,
    bool IsTargeted,
    long Version,
    DateTime CreatedAtUtc);
