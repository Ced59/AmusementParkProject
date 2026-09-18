using AmusementPark.Core.Domain.Trips;

namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripInvitationPreviewResult(
    string TripTitle,
    string InviterDisplayName,
    TripDelegatedRole ProposedRole,
    TripInvitationPeriodKind PeriodKind,
    string? StartMonth,
    string? EndMonth,
    TripInvitationMemberCountBand MemberCountBand,
    DateTime ExpiresAtUtc,
    bool IsTargeted);
