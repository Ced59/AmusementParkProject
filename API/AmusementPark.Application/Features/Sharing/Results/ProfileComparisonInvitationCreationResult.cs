using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record ProfileComparisonInvitationCreationResult(
    string Token,
    DateTime ExpiresAtUtc,
    IReadOnlyCollection<ProfileComparisonCategory> Categories);
