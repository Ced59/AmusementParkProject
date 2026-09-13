using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record ProfileComparisonInvitationPreviewResult(
    ProfileComparisonInvitationPreviewStatus Status,
    string? CreatorDisplayName,
    string? InviteeDisplayName,
    DateTime ExpiresAtUtc,
    DateTime? AcceptedAtUtc,
    IReadOnlyCollection<ProfileComparisonCategory> Categories,
    bool CanAccept);
