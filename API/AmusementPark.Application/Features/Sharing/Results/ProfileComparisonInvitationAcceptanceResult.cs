using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record ProfileComparisonInvitationAcceptanceResult(
    string ShareId,
    DateTime AcceptedAtUtc,
    IReadOnlyCollection<ProfileComparisonCategory> Categories);
