using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RatingRankingMutationRecoveryTarget
{
    public RatingRankingMutationRecoveryTarget(
        RatingTargetType targetType,
        string targetId,
        string userId,
        string mutationToken)
    {
        if (targetType is not RatingTargetType.Park and not RatingTargetType.ParkItem)
        {
            throw new ArgumentOutOfRangeException(nameof(targetType));
        }

        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("A recovery target identifier is required.", nameof(targetId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("A recovery user identifier is required.", nameof(userId));
        }

        if (!Guid.TryParseExact(mutationToken, "N", out Guid parsedMutationToken))
        {
            throw new ArgumentException("The rating mutation fence token is invalid.", nameof(mutationToken));
        }

        this.TargetType = targetType;
        this.TargetId = targetId.Trim();
        this.UserId = userId.Trim();
        this.MutationToken = parsedMutationToken.ToString("N");
    }

    public RatingTargetType TargetType { get; }

    public string TargetId { get; }

    public string UserId { get; }

    public string MutationToken { get; }
}
