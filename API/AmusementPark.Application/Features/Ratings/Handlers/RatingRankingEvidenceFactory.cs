using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Handlers;

internal static class RatingRankingEvidenceFactory
{
    public static ParkRankingEvaluation? TryCreateParkEvaluation(
        RatingRankingItemResult? directParkSource,
        IReadOnlyCollection<RatingRankingItemResult> itemSources,
        ParkRankingContributorFacts contributorFacts,
        IReadOnlyCollection<PublicParkItemEvidenceFact> publicItems,
        bool isSingleCategoryParkException,
        IReadOnlyCollection<ParkRatingRankingCategoryResult> categories,
        RankingEligibilityPolicy eligibilityPolicy)
    {
        if (!TryConvertEvidenceCounts(contributorFacts, out ParkContributorDomainCounts counts))
        {
            return null;
        }

        HashSet<string> publicItemIds = publicItems
            .Select(static item => item.TargetId)
            .ToHashSet(StringComparer.Ordinal);
        if (itemSources.Any(source => !publicItemIds.Contains(source.TargetId)))
        {
            return null;
        }

        if (directParkSource is not null && !directParkSource.UniqueContributorCount.HasValue)
        {
            return null;
        }

        Dictionary<string, RankingEvidence> itemEvidenceById = new Dictionary<string, RankingEvidence>(
            StringComparer.Ordinal);
        foreach (IGrouping<string, RatingRankingItemResult> itemSourceGroup in itemSources.GroupBy(
                     static source => source.TargetId,
                     StringComparer.Ordinal))
        {
            if (itemSourceGroup.Count() != 1)
            {
                return null;
            }

            RatingRankingItemResult itemSource = itemSourceGroup.Single();
            if (!itemSource.AggregateIntegrityIsValid.HasValue
                || !itemSource.UniqueContributorCount.HasValue)
            {
                return null;
            }

            RankingEvidence? itemEvidence = RatingResultFactory.TryCreateSimpleDomainEvidence(
                itemSource.UniqueContributorCount.Value,
                itemSource.RatingCount,
                targetCanReceiveVisitorRatings: true,
                aggregateIntegrityIsValid: itemSource.AggregateIntegrityIsValid.Value,
                eligibilityPolicy);
            if (itemEvidence is null)
            {
                return null;
            }

            itemEvidenceById.Add(itemSource.TargetId, itemEvidence);
        }

        if (publicItemIds.Count != publicItems.Count)
        {
            return null;
        }

        List<RankingCategoryCoverage> categoryCoverage = publicItems
            .GroupBy(static item => item.Category)
            .Select(group => new RankingCategoryCoverage(
                group.Count(),
                group.Count(item => itemEvidenceById.TryGetValue(item.TargetId, out RankingEvidence? evidence)
                    && evidence.IsEligibleForMainRanking)))
            .ToList();

        if (!TrySumObservationCounts(directParkSource, itemSources, out long sourceObservationCount))
        {
            return null;
        }

        bool? sourceAggregateIntegrity = TryResolveAggregateIntegrity(directParkSource, itemSources);
        if (!sourceAggregateIntegrity.HasValue)
        {
            return null;
        }

        bool sourceContributorCountsAreConsistent = (directParkSource is null
                || directParkSource.UniqueContributorCount == directParkSource.RatingCount)
            && itemSources.All(static source => source.UniqueContributorCount == source.RatingCount);
        bool aggregateIntegrityIsValid = sourceAggregateIntegrity.Value
            && sourceContributorCountsAreConsistent
            && sourceObservationCount == contributorFacts.RatingObservationCount
            && (directParkSource?.UniqueContributorCount ?? 0) == contributorFacts.DirectParkContributorCount;
        ParkRankingEvidenceInput input = new ParkRankingEvidenceInput(
            counts.UniqueContributorCount,
            counts.RatingObservationCount,
            counts.DirectParkContributorCount,
            counts.ItemContributorCount,
            categoryCoverage,
            IsSingleCategoryParkException: isSingleCategoryParkException,
            TargetCanReceiveVisitorRatings: true,
            IsExcludedByModeration: false,
            aggregateIntegrityIsValid);
        double? itemsScore = categories.Count == 0
            ? null
            : RatingScoreCalculator.CalculateCategoryBalancedItemsScore(
                categories.Select(static category => category.BayesianScore).ToList());
        if (!eligibilityPolicy.TryEvaluateParkRanking(
                input,
                directParkSource?.BayesianScore,
                itemsScore,
                out ParkRankingEvaluation? evaluation)
            || evaluation is null)
        {
            return null;
        }

        return evaluation;
    }

    public static IReadOnlyCollection<RatingRankingItemResult> ApplyVerifiedAggregateIntegrity(
        IReadOnlyCollection<RatingRankingItemResult> sources,
        IReadOnlyCollection<RatingAggregateSourceFact> aggregateSourceFacts,
        bool sourceFactsWereRead)
    {
        if (!sourceFactsWereRead)
        {
            return sources;
        }

        IReadOnlyDictionary<string, RatingAggregateSourceFact> sourceFactsByTarget = aggregateSourceFacts
            .GroupBy(
                static fact => BuildTargetKey(fact.TargetType, fact.TargetId),
                StringComparer.Ordinal)
            .Where(static group => group.Count() == 1)
            .ToDictionary(static group => group.Key, static group => group.Single(), StringComparer.Ordinal);

        return sources.Select(source =>
        {
            bool? aggregateIntegrityIsValid = source.AggregateIntegrityIsValid;
            if (!aggregateIntegrityIsValid.HasValue)
            {
                return source;
            }

            sourceFactsByTarget.TryGetValue(
                BuildTargetKey(source.TargetType, source.TargetId),
                out RatingAggregateSourceFact? sourceFact);
            long verifiedUniqueContributorCount = 0;
            bool sourceProjectionIsValid = sourceFact is not null
                && RatingAggregate.TryResolveVerifiedSourceProjection(
                    source.RatingCount,
                    source.UniqueContributorCount,
                    source.RatingSum,
                    source.AverageRating,
                    source.BayesianScore,
                    sourceFact.RatingObservationCount,
                    sourceFact.UniqueContributorCount,
                    sourceFact.RatingSum,
                    out verifiedUniqueContributorCount);

            return source with
            {
                AggregateIntegrityIsValid = aggregateIntegrityIsValid.Value && sourceProjectionIsValid,
                UniqueContributorCount = sourceProjectionIsValid
                    ? verifiedUniqueContributorCount
                    : source.UniqueContributorCount,
            };
        }).ToList();
    }

    private static bool? TryResolveAggregateIntegrity(
        RatingRankingItemResult? directParkSource,
        IReadOnlyCollection<RatingRankingItemResult> itemSources)
    {
        IEnumerable<RatingRankingItemResult> aggregateSources = directParkSource is null
            ? itemSources
            : itemSources.Prepend(directParkSource);
        List<bool?> integrityFacts = aggregateSources
            .Select(static source => source.AggregateIntegrityIsValid)
            .ToList();
        if (integrityFacts.Any(static fact => !fact.HasValue))
        {
            return null;
        }

        return integrityFacts.All(static fact => fact!.Value);
    }

    private static string BuildTargetKey(RatingTargetType targetType, string targetId)
    {
        return $"{targetType}:{targetId}";
    }

    private static bool TryConvertEvidenceCounts(
        ParkRankingContributorFacts facts,
        out ParkContributorDomainCounts counts)
    {
        if (facts.UniqueContributorCount < 0
            || facts.UniqueContributorCount > int.MaxValue
            || facts.RatingObservationCount < 0
            || facts.RatingObservationCount > int.MaxValue
            || facts.DirectParkContributorCount < 0
            || facts.DirectParkContributorCount > int.MaxValue
            || facts.ItemContributorCount < 0
            || facts.ItemContributorCount > int.MaxValue)
        {
            counts = default;
            return false;
        }

        counts = new ParkContributorDomainCounts(
            checked((int)facts.UniqueContributorCount),
            checked((int)facts.RatingObservationCount),
            checked((int)facts.DirectParkContributorCount),
            checked((int)facts.ItemContributorCount));
        return true;
    }

    private static bool TrySumObservationCounts(
        RatingRankingItemResult? directParkSource,
        IReadOnlyCollection<RatingRankingItemResult> itemSources,
        out long sourceObservationCount)
    {
        sourceObservationCount = directParkSource?.RatingCount ?? 0;
        if (sourceObservationCount < 0 || sourceObservationCount > int.MaxValue)
        {
            return false;
        }

        foreach (RatingRankingItemResult itemSource in itemSources)
        {
            if (itemSource.RatingCount < 0
                || itemSource.RatingCount > int.MaxValue
                || sourceObservationCount > int.MaxValue - itemSource.RatingCount)
            {
                sourceObservationCount = 0;
                return false;
            }

            sourceObservationCount += itemSource.RatingCount;
        }

        return true;
    }

    private readonly record struct ParkContributorDomainCounts(
        int UniqueContributorCount,
        int RatingObservationCount,
        int DirectParkContributorCount,
        int ItemContributorCount);
}
