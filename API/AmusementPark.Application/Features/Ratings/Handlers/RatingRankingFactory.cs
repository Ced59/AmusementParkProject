using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Ratings.Results;
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

internal static class RatingRankingFactory
{
    public static IReadOnlyCollection<ParkRatingRankingResult> BuildParkRankings(
        IReadOnlyCollection<RatingRankingItemResult> sources,
        ParkItemCategory? categoryFilter = null)
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
            .Select(static (ranking, index) => ranking with { Rank = index + 1 })
            .ToList();

        return rankings;
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
            .Select(static (source, index) => new ParkItemRatingRankingResult(
                index + 1,
                source.TargetId,
                source.TargetName,
                source.ParkId,
                source.ParkName!.Trim(),
                source.ParkItemCategory!.Value,
                source.ParkItemType,
                source.RatingCount,
                source.AverageRating,
                source.BayesianScore))
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
