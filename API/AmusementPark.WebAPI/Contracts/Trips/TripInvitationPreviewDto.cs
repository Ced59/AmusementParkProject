using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed record TripInvitationPreviewDto(
    string TripTitle,
    string InviterDisplayName,
    TripDelegatedRole ProposedRole,
    TripInvitationPeriodKind PeriodKind,
    string? StartMonth,
    string? EndMonth,
    TripInvitationMemberCountBand MemberCountBand,
    DateTime ExpiresAtUtc,
    bool IsTargeted);
