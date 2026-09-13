using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Results;

public sealed record ProfileComparisonSummaryResult(
    string ShareId,
    string? OtherDisplayName,
    DateTime CreatedAtUtc,
    IReadOnlyCollection<ProfileComparisonCategory> Categories,
    bool IsModerationSuspended);
