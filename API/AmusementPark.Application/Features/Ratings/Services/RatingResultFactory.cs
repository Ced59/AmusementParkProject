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
        bool? aggregateIntegrityIsValid)
    {
        long ratingCount = aggregate?.RatingCount ?? 0;
        long? uniqueContributorCount = aggregate?.UniqueContributorCount
            ?? (aggregate is null ? 0 : null);
        bool? resolvedAggregateIntegrity = aggregateIntegrityIsValid ?? aggregate?.IsCalculationCurrent;
        RankingEvidenceResult? evidence = resolvedAggregateIntegrity.HasValue
            && uniqueContributorCount.HasValue
            ? TryCreateSimpleEvidence(
                uniqueContributorCount.Value,
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
        long uniqueContributorCount,
        long ratingObservationCount,
        bool targetCanReceiveVisitorRatings,
        bool aggregateIntegrityIsValid)
    {
        if (!TryConvertToDomainCount(uniqueContributorCount, out int boundedUniqueContributorCount)
            || !TryConvertToDomainCount(ratingObservationCount, out int boundedRatingObservationCount))
        {
            return null;
        }

        RankingEvidence evidence = EligibilityPolicy.EvaluateSimpleTarget(
            new SimpleRankingEvidenceInput(
                boundedUniqueContributorCount,
                boundedRatingObservationCount,
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
