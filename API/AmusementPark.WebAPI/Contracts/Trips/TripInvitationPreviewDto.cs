namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed record TripInvitationPreviewDto(
    string TripTitle,
    string InviterDisplayName,
    TripDelegatedRoleDto ProposedRole,
    TripInvitationPeriodKindDto PeriodKind,
    string? StartMonth,
    string? EndMonth,
    TripInvitationMemberCountBandDto MemberCountBand,
    DateTime ExpiresAtUtc,
    bool IsTargeted);
