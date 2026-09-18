using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed record TripInvitationSummaryDto(
    string InvitationId,
    string TokenHint,
    TripDelegatedRole ProposedRole,
    DateTime ExpiresAtUtc,
    bool IsTargeted,
    long Version,
    DateTime CreatedAtUtc);
