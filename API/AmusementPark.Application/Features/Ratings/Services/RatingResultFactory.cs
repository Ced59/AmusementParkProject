using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Services;

public static class RatingResultFactory
{
    private static readonly RankingEligibilityPolicy EligibilityPolicy = RankingEligibilityPolicy.Initial;

    public static RatingSummaryResult CreateSummary(
        RatingTargetType targetType,
        string targetId,
        RatingAggregate? aggregate,
        bool targetCanReceiveVisitorRatings,
        bool? aggregateIntegrityIsValid = null)
    {
        long ratingCount = aggregate?.RatingCount ?? 0;
        bool? resolvedAggregateIntegrity = aggregateIntegrityIsValid ?? aggregate?.IsCalculationCurrent;
        RankingEvidenceResult? evidence = aggregate is null
            ? TryCreateSimpleEvidence(
                ratingCount,
                targetCanReceiveVisitorRatings,
                aggregateIntegrityIsValid: true)
            : resolvedAggregateIntegrity.HasValue
                ? TryCreateSimpleEvidence(
                    ratingCount,
                    targetCanReceiveVisitorRatings,
                    resolvedAggregateIntegrity.Value)
                : null;

        return new RatingSummaryResult(
            aggregate?.TargetType ?? targetType,
            aggregate?.TargetId ?? targetId,
            ratingCount,
            aggregate?.AverageRating ?? 0d,
            aggregate?.BayesianScore ?? RatingScoreCalculator.PriorMean)
        {
            Evidence = evidence,
        };
    }

    public static RankingEvidenceResult? TryCreateSimpleEvidence(
        long ratingCount,
        bool targetCanReceiveVisitorRatings,
        bool aggregateIntegrityIsValid)
    {
        if (!TryConvertToDomainCount(ratingCount, out int boundedRatingCount))
        {
            return null;
        }

        RankingEvidence evidence = EligibilityPolicy.EvaluateSimpleTarget(
            new SimpleRankingEvidenceInput(
                boundedRatingCount,
                boundedRatingCount,
                targetCanReceiveVisitorRatings,
                IsExcludedByModeration: false,
                aggregateIntegrityIsValid));

        return ToResult(evidence);
    }

    public static RankingEvidenceResult ToResult(RankingEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        return new RankingEvidenceResult(
            evidence.Level,
            evidence.IsEligibleForMainRanking,
            evidence.UniqueContributorCount,
            evidence.RatingObservationCount,
            evidence.DirectParkContributorCount,
            evidence.ItemContributorCount,
            evidence.EligibleItemCount,
            evidence.EligibleCategoryCount,
            evidence.MethodologyVersion,
            evidence.IneligibilityReason,
            evidence.NextContributorThreshold);
    }

    internal static bool TryConvertToDomainCount(long value, out int result)
    {
        if (value < 0 || value > int.MaxValue)
        {
            result = 0;
            return false;
        }

        result = checked((int)value);
        return true;
    }
}
