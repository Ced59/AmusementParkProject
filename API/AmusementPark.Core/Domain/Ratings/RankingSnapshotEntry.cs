using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Entrée éligible d'un classement matérialisé. Les cibles sans rang restent dans les agrégats sources.
/// </summary>
public sealed class RankingSnapshotEntry
{
    public RankingSnapshotEntry(
        int rank,
        RatingTargetType targetType,
        string targetId,
        double score,
        RankingEvidence evidence)
        : this(rank, rank, targetType, targetId, null, score, evidence)
    {
    }

    public RankingSnapshotEntry(
        int position,
        int rank,
        RatingTargetType targetType,
        string targetId,
        double score,
        RankingEvidence evidence)
        : this(position, rank, targetType, targetId, null, score, evidence)
    {
    }

    public RankingSnapshotEntry(
        int position,
        int rank,
        RatingTargetType targetType,
        string targetId,
        ParkItemCategory? parkItemCategory,
        double score,
        RankingEvidence evidence)
    {
        if (position <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(position));
        }

        if (rank <= 0 || rank > position)
        {
            throw new ArgumentOutOfRangeException(nameof(rank));
        }

        if (!Enum.IsDefined(targetType))
        {
            throw new ArgumentOutOfRangeException(nameof(targetType));
        }

        if (targetType == RatingTargetType.ParkItem &&
            (!parkItemCategory.HasValue || !Enum.IsDefined(parkItemCategory.Value)))
        {
            throw new ArgumentException(
                "A park-item ranking entry must preserve its category.",
                nameof(parkItemCategory));
        }

        if (targetType == RatingTargetType.Park && parkItemCategory.HasValue)
        {
            throw new ArgumentException(
                "A park ranking entry cannot have a park-item category.",
                nameof(parkItemCategory));
        }

        string normalizedTargetId = IdentifierRules.NormalizeRequired(targetId, nameof(targetId));
        if (!double.IsFinite(score) ||
            score < RatingValue.MinimumHalfSteps / 2d ||
            score > RatingValue.MaximumHalfSteps / 2d)
        {
            throw new ArgumentOutOfRangeException(nameof(score));
        }

        ArgumentNullException.ThrowIfNull(evidence);
        _ = evidence.MethodologyVersion.Value;
        if (!RankingEligibilityPolicy.TryResolve(
                evidence.MethodologyVersion,
                out RankingEligibilityPolicy? eligibilityPolicy) ||
            !eligibilityPolicy.IsEligibleSnapshotEvidence(targetType, evidence))
        {
            throw new ArgumentException(
                "A ranking snapshot can contain only evidence derived from its versioned policy.",
                nameof(evidence));
        }

        this.Position = position;
        this.Rank = rank;
        this.TargetType = targetType;
        this.TargetId = normalizedTargetId;
        this.ParkItemCategory = parkItemCategory;
        this.Score = score;
        this.Evidence = evidence;
    }

    public int Position { get; }

    public int Rank { get; }

    public RatingTargetType TargetType { get; }

    public string TargetId { get; }

    public ParkItemCategory? ParkItemCategory { get; }

    public double Score { get; }

    public RankingEvidence Evidence { get; }
}
