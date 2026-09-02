using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Handlers;

internal static class RatingRankingPaging
{
    public static PagedResult<T> BuildPage<T>(IReadOnlyCollection<T> rankings, int page, int pageSize)
    {
        IReadOnlyCollection<T> pageItems = rankings
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PagedResult<T>(pageItems, page, pageSize, rankings.Count);
    }
}

internal sealed record ParkRankingSnapshotCandidate(
    ParkRatingRankingResult Ranking,
    RankingEvidence? Evidence,
    ParkItemComponentEligibility? ItemComponent);

internal sealed record ParkItemRankingSnapshotCandidate(
    ParkItemRatingRankingResult Ranking,
    RankingEvidence? Evidence);

internal static class RatingRankingFactory
{
    public static IReadOnlyCollection<ParkRatingRankingResult> BuildParkRankings(
        IReadOnlyCollection<RatingRankingItemResult> sources,
        ParkItemCategory? categoryFilter = null,
        ParkRankingEvidenceFactsBatch? evidenceFacts = null)
    {
        List<ParkRatingRankingResult> rankings = sources
            .Where(static source => !string.IsNullOrWhiteSpace(source.ParkId))
            .GroupBy(static source => source.ParkId, StringComparer.Ordinal)
            .Select(group => BuildParkRanking(group.Key, group.ToList(), categoryFilter))
            .Where(static ranking => ranking is not null)
            .Select(static ranking => ranking!)
            .OrderByDescending(static ranking => ranking.Score)
            .ThenByDescending(static ranking => ranking.RatingCount)
            .ThenBy(static ranking => ranking.ParkName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static ranking => ranking.ParkId, StringComparer.Ordinal)
            .Select(static (ranking, index) => ranking with { Rank = index + 1 })
            .ToList();

        if (evidenceFacts is null)
        {
            return rankings;
        }

        IReadOnlyCollection<ParkRankingSnapshotCandidate> candidates =
            BuildParkSnapshotCandidates(rankings, sources, evidenceFacts, categoryFilter);
        return MapParkEvidence(OrderParkSnapshotCandidates(candidates));
    }

    public static IReadOnlyCollection<ParkRatingRankingResult> ApplyParkEvidence(
        IReadOnlyCollection<ParkRatingRankingResult> rankings,
        IReadOnlyCollection<RatingRankingItemResult> sources,
        ParkRankingEvidenceFactsBatch evidenceFacts,
        ParkItemCategory? categoryFilter = null)
    {
        IReadOnlyCollection<ParkRankingSnapshotCandidate> candidates =
            BuildParkSnapshotCandidates(
                rankings,
                sources,
                evidenceFacts,
                categoryFilter,
                replaceScoreWithPolicyScore: false);
        return MapParkEvidence(candidates);
    }

    private static IReadOnlyCollection<ParkRatingRankingResult> MapParkEvidence(
        IReadOnlyCollection<ParkRankingSnapshotCandidate> candidates)
    {
        return candidates
            .Select(static candidate => candidate.Ranking with
            {
                Evidence = candidate.Evidence is null
                    ? null
                    : RatingResultFactory.ToResult(candidate.Evidence),
            })
            .ToList();
    }

    internal static IReadOnlyCollection<ParkRankingSnapshotCandidate> BuildParkSnapshotCandidates(
        IReadOnlyCollection<ParkRatingRankingResult> rankings,
        IReadOnlyCollection<RatingRankingItemResult> sources,
        ParkRankingEvidenceFactsBatch evidenceFacts,
        ParkItemCategory? categoryFilter = null,
        RankingEligibilityPolicy? eligibilityPolicy = null,
        bool replaceScoreWithPolicyScore = true)
    {
        ArgumentNullException.ThrowIfNull(rankings);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(evidenceFacts);

        IReadOnlyDictionary<string, IReadOnlyCollection<RatingRankingItemResult>> sourcesByPark = sources
            .Where(static source => !string.IsNullOrWhiteSpace(source.ParkId))
            .GroupBy(static source => source.ParkId, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => (IReadOnlyCollection<RatingRankingItemResult>)group.ToList(),
                StringComparer.Ordinal);
        IReadOnlyDictionary<string, ParkRankingContributorFacts> contributorFactsByPark = evidenceFacts.Contributors
            .GroupBy(static facts => facts.ParkId, StringComparer.Ordinal)
            .Where(static group => group.Count() == 1)
            .ToDictionary(static group => group.Key, static group => group.Single(), StringComparer.Ordinal);
        ILookup<string, PublicParkItemEvidenceFact> publicItemsByPark = evidenceFacts.PublicItems
            .ToLookup(static item => item.ParkId, StringComparer.Ordinal);
        HashSet<string> incompletePublicInventoryParkIds = evidenceFacts.IncompletePublicInventoryParkIds
            .ToHashSet(StringComparer.Ordinal);

        return rankings.Select(ranking =>
        {
            if (!sourcesByPark.TryGetValue(ranking.ParkId, out IReadOnlyCollection<RatingRankingItemResult>? parkSources)
                || !contributorFactsByPark.TryGetValue(ranking.ParkId, out ParkRankingContributorFacts? contributorFacts)
                || incompletePublicInventoryParkIds.Contains(ranking.ParkId))
            {
                return new ParkRankingSnapshotCandidate(ranking, null, null);
            }

            parkSources = RatingRankingEvidenceFactory.ApplyVerifiedAggregateIntegrity(
                parkSources,
                evidenceFacts.AggregateSources,
                evidenceFacts.AggregateSourceFactsWereRead);

            List<RatingRankingItemResult> directParkSources = parkSources
                .Where(static source => source.TargetType == RatingTargetType.Park)
                .ToList();
            IReadOnlyCollection<RatingRankingItemResult> itemSources = parkSources
                .Where(static source => source.TargetType == RatingTargetType.ParkItem
                    && source.ParkItemCategory.HasValue)
                .ToList();
            IReadOnlyCollection<PublicParkItemEvidenceFact> allPublicItems = publicItemsByPark[ranking.ParkId]
                .ToList();
            IReadOnlyCollection<PublicParkItemEvidenceFact> publicItems = allPublicItems
                .Where(item => !categoryFilter.HasValue || item.Category == categoryFilter.Value)
                .ToList();
            bool isSingleCategoryParkException = allPublicItems
                .Select(static item => item.Category)
                .Distinct()
                .Count() == 1;

            ParkRankingEvaluation? evaluation = directParkSources.Count <= 1
                ? RatingRankingEvidenceFactory.TryCreateParkEvaluation(
                    directParkSources.FirstOrDefault(),
                    itemSources,
                    contributorFacts,
                    publicItems,
                    isSingleCategoryParkException,
                    ranking.Categories,
                    eligibilityPolicy ?? RankingEligibilityPolicy.Initial)
                : null;
            return new ParkRankingSnapshotCandidate(
                evaluation is null || !replaceScoreWithPolicyScore
                    ? ranking
                    : ranking with { Score = evaluation.Score },
                evaluation?.Evidence,
                evaluation?.ItemComponent);
        }).ToList();
    }

    internal static IReadOnlyCollection<ParkRankingSnapshotCandidate> OrderParkSnapshotCandidates(
        IReadOnlyCollection<ParkRankingSnapshotCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        return candidates
            .OrderByDescending(static candidate => candidate.Ranking.Score)
            .ThenByDescending(static candidate => candidate.Ranking.RatingCount)
            .ThenBy(static candidate => candidate.Ranking.ParkName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static candidate => candidate.Ranking.ParkId, StringComparer.Ordinal)
            .Select(static (candidate, index) => candidate with
            {
                Ranking = candidate.Ranking with { Rank = index + 1 },
            })
            .ToList();
    }

    public static IReadOnlyCollection<ParkItemRatingRankingResult> ApplyParkItemEvidence(
        IReadOnlyCollection<ParkItemRatingRankingResult> rankings,
        IReadOnlyCollection<RatingRankingItemResult> sources,
        IReadOnlyCollection<RatingAggregateSourceFact> aggregateSourceFacts)
    {
        return BuildParkItemSnapshotCandidates(rankings, sources, aggregateSourceFacts)
            .Select(static candidate => candidate.Ranking with
            {
                Evidence = candidate.Evidence is null
                    ? null
                    : RatingResultFactory.ToResult(candidate.Evidence),
            })
            .ToList();
    }

    internal static IReadOnlyCollection<ParkItemRankingSnapshotCandidate> BuildParkItemSnapshotCandidates(
        IReadOnlyCollection<ParkItemRatingRankingResult> rankings,
        IReadOnlyCollection<RatingRankingItemResult> sources,
        IReadOnlyCollection<RatingAggregateSourceFact> aggregateSourceFacts,
        RankingEligibilityPolicy? eligibilityPolicy = null)
    {
        ArgumentNullException.ThrowIfNull(rankings);
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(aggregateSourceFacts);

        IReadOnlyCollection<RatingRankingItemResult> verifiedSources =
            RatingRankingEvidenceFactory.ApplyVerifiedAggregateIntegrity(
            sources,
            aggregateSourceFacts,
            sourceFactsWereRead: true);
        IReadOnlyDictionary<string, RatingRankingItemResult> sourceByTargetId = verifiedSources
            .Where(static source => source.TargetType == RatingTargetType.ParkItem)
            .GroupBy(static source => source.TargetId, StringComparer.Ordinal)
            .Where(static group => group.Count() == 1)
            .ToDictionary(static group => group.Key, static group => group.Single(), StringComparer.Ordinal);

        return rankings.Select(ranking =>
        {
            if (!sourceByTargetId.TryGetValue(ranking.TargetId, out RatingRankingItemResult? source)
                || !source.AggregateIntegrityIsValid.HasValue
                || !source.UniqueContributorCount.HasValue)
            {
                return new ParkItemRankingSnapshotCandidate(ranking, null);
            }

            RankingEvidence? evidence = RatingResultFactory.TryCreateSimpleDomainEvidence(
                    source.UniqueContributorCount.Value,
                    source.RatingCount,
                    targetCanReceiveVisitorRatings: true,
                    aggregateIntegrityIsValid: source.AggregateIntegrityIsValid.Value,
                    eligibilityPolicy ?? RankingEligibilityPolicy.Initial);
            return new ParkItemRankingSnapshotCandidate(ranking, evidence);
        }).ToList();
    }

    public static IReadOnlyCollection<ParkItemRatingRankingResult> BuildParkItemRankings(
        IReadOnlyCollection<RatingRankingItemResult> sources,
        ParkItemType? parkItemType = null)
    {
        return sources
            .Where(static source =>
                source.TargetType == RatingTargetType.ParkItem
                && source.ParkItemCategory.HasValue
                && !string.IsNullOrWhiteSpace(source.ParkId)
                && !string.IsNullOrWhiteSpace(source.ParkName))
            .Where(source => !parkItemType.HasValue || source.ParkItemType == parkItemType.Value)
            .OrderByDescending(static source => source.BayesianScore)
            .ThenByDescending(static source => source.RatingCount)
            .ThenByDescending(static source => source.AverageRating)
            .ThenBy(static source => source.TargetName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static source => source.TargetId, StringComparer.Ordinal)
            .Select(static (source, index) =>
            {
                return new ParkItemRatingRankingResult(
                    index + 1,
                    source.TargetId,
                    source.TargetName,
                    source.ParkId,
                    source.ParkName!.Trim(),
                    source.ParkItemCategory!.Value,
                    source.ParkItemType,
                    source.RatingCount,
                    source.AverageRating,
                    source.BayesianScore)
                {
                    Evidence = source.AggregateIntegrityIsValid.HasValue
                        && source.UniqueContributorCount.HasValue
                        ? RatingResultFactory.TryCreateSimpleEvidence(
                            source.UniqueContributorCount.Value,
                            source.RatingCount,
                            targetCanReceiveVisitorRatings: true,
                            aggregateIntegrityIsValid: source.AggregateIntegrityIsValid.Value)
                        : null,
                };
            })
            .ToList();
    }

    public static IReadOnlyCollection<UserParkRatingRankingResult> BuildUserParkRankings(
        IReadOnlyCollection<UserRatingListItemResult> sources)
    {
        return sources
            .Where(static source => !string.IsNullOrWhiteSpace(source.ParkId))
            .GroupBy(static source => source.ParkId, StringComparer.Ordinal)
            .Select(static group => BuildUserParkRanking(group.Key, group.ToList()))
            .OrderByDescending(static ranking => ranking.AverageRating)
            .ThenByDescending(static ranking => ranking.RatingCount)
            .ThenBy(static ranking => ranking.ParkName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static ranking => ranking.ParkId, StringComparer.Ordinal)
            .Select(static (ranking, index) => ranking with { Rank = index + 1 })
            .ToList();
    }

    public static IReadOnlyCollection<UserParkItemRatingRankingResult> BuildUserParkItemRankings(
        IReadOnlyCollection<UserRatingListItemResult> sources,
        ParkItemCategory category,
        ParkItemType? parkItemType = null)
    {
        return sources
            .Where(source =>
                source.TargetType == RatingTargetType.ParkItem
                && source.ParkItemCategory == category
                && (!parkItemType.HasValue || source.ParkItemType == parkItemType.Value))
            .OrderByDescending(static source => source.Value)
            .ThenBy(static source => source.TargetName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static source => source.ParkName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static source => source.TargetId, StringComparer.Ordinal)
            .Select(static (source, index) => new UserParkItemRatingRankingResult(index + 1, source))
            .ToList();
    }

    private static ParkRatingRankingResult? BuildParkRanking(
        string parkId,
        IReadOnlyCollection<RatingRankingItemResult> sources,
        ParkItemCategory? categoryFilter)
    {
        RatingRankingItemResult? directParkSource = sources.FirstOrDefault(static source => source.TargetType == RatingTargetType.Park);
        List<RatingRankingItemResult> itemSources = sources
            .Where(static source => source.TargetType == RatingTargetType.ParkItem && source.ParkItemCategory.HasValue)
            .ToList();

        if (categoryFilter.HasValue && itemSources.Count == 0)
        {
            return null;
        }

        List<ParkRatingRankingCategoryResult> categories = itemSources
            .GroupBy(static source => source.ParkItemCategory!.Value)
            .Select(static group => BuildCategoryRanking(group.Key, group.ToList()))
            .OrderByDescending(static category => category.BayesianScore)
            .ThenBy(static category => category.ParkItemCategory)
            .ToList();

        double? directParkScore = directParkSource?.BayesianScore;
        double? itemsScore = categories.Count == 0
            ? null
            : RatingScoreCalculator.CalculateCategoryBalancedItemsScore(categories.Select(static category => category.BayesianScore).ToList());
        double score = RatingScoreCalculator.CalculateCompositeParkScore(directParkScore, itemsScore);
        long parkRatingCount = directParkSource?.RatingCount ?? 0;
        long itemRatingCount = itemSources.Sum(static source => source.RatingCount);
        long totalRatingCount = parkRatingCount + itemRatingCount;
        double itemRatingSum = itemSources.Sum(static source => source.RatingSum);
        string parkName = directParkSource?.TargetName
            ?? sources.Select(static source => source.ParkName).FirstOrDefault(static name => !string.IsNullOrWhiteSpace(name))?.Trim()
            ?? parkId;

        return new ParkRatingRankingResult(
            0,
            parkId,
            parkName,
            totalRatingCount,
            score,
            parkRatingCount,
            directParkSource?.AverageRating ?? 0d,
            itemRatingCount,
            RatingScoreCalculator.CalculateAverage(itemRatingSum, itemRatingCount),
            categories);
    }

    private static UserParkRatingRankingResult BuildUserParkRanking(
        string parkId,
        IReadOnlyCollection<UserRatingListItemResult> sources)
    {
        UserRatingListItemResult? parkRating = sources.FirstOrDefault(static source =>
            source.TargetType == RatingTargetType.Park);
        List<UserRatingListItemResult> itemRatings = sources
            .Where(static source =>
                source.TargetType == RatingTargetType.ParkItem
                && source.ParkItemCategory.HasValue)
            .ToList();
        List<UserParkRatingRankingCategoryResult> categories = itemRatings
            .GroupBy(static source => source.ParkItemCategory!.Value)
            .OrderBy(static group => group.Key)
            .Select(static group =>
            {
                List<UserRatingListItemResult> ratings = group
                    .OrderByDescending(static source => source.Value)
                    .ThenBy(static source => source.TargetName, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                return new UserParkRatingRankingCategoryResult(
                    group.Key,
                    ratings.Average(static source => source.Value),
                    ratings);
            })
            .ToList();
        string parkName = parkRating?.TargetName
            ?? sources.Select(static source => source.ParkName)
                .FirstOrDefault(static name => !string.IsNullOrWhiteSpace(name))?.Trim()
            ?? parkId;

        return new UserParkRatingRankingResult(
            0,
            parkId,
            parkName,
            sources.Count,
            sources.Average(static source => source.Value),
            parkRating,
            categories);
    }

    private static ParkRatingRankingCategoryResult BuildCategoryRanking(ParkItemCategory category, IReadOnlyCollection<RatingRankingItemResult> sources)
    {
        long ratingCount = sources.Sum(static source => source.RatingCount);
        double ratingSum = sources.Sum(static source => source.RatingSum);
        List<ParkRatingRankingItemResult> items = sources
            .OrderByDescending(static source => source.BayesianScore)
            .ThenByDescending(static source => source.RatingCount)
            .ThenBy(static source => source.TargetName, StringComparer.OrdinalIgnoreCase)
            .Select(static source => new ParkRatingRankingItemResult(
                source.TargetId,
                source.TargetName,
                source.ParkItemCategory,
                source.ParkItemType,
                source.RatingCount,
                source.AverageRating,
                source.BayesianScore))
            .ToList();

        return new ParkRatingRankingCategoryResult(
            category,
            ratingCount,
            RatingScoreCalculator.CalculateAverage(ratingSum, ratingCount),
            RatingScoreCalculator.CalculateBayesianScore(ratingSum, ratingCount),
            items);
    }
}
