using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Models;

public sealed record RatingRankingRecoveredMutation
{
    public RatingRankingRecoveredMutation(
        string recoveryToken,
        RatingTargetType targetType,
        string targetId,
        string userId,
        string mutationToken)
    {
        if (!Guid.TryParseExact(recoveryToken, "N", out Guid parsedToken))
        {
            throw new ArgumentException("The ranking recovery token is invalid.", nameof(recoveryToken));
        }

        RatingRankingMutationRecoveryTarget recoveryTarget =
            new RatingRankingMutationRecoveryTarget(
                targetType,
                targetId,
                userId,
                mutationToken);
        this.RecoveryToken = parsedToken.ToString("N");
        this.TargetType = recoveryTarget.TargetType;
        this.TargetId = recoveryTarget.TargetId;
        this.UserId = recoveryTarget.UserId;
        this.MutationToken = recoveryTarget.MutationToken;
    }

    public string RecoveryToken { get; }

    public RatingTargetType TargetType { get; }

    public string TargetId { get; }

    public string UserId { get; }

    public string MutationToken { get; }
}
