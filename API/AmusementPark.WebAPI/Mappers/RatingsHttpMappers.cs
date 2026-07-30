using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.WebAPI.Contracts.Ratings;

namespace AmusementPark.WebAPI.Mappers;

internal static class RatingsHttpMappers
{
    public static RatingTargetType ToRatingTargetType(this string? value)
    {
        return Enum.TryParse(value, true, out RatingTargetType parsed) ? parsed : default;
    }

    public static ParkItemCategory? ToParkItemCategoryFilter(this string? value)
    {
        return Enum.TryParse(value, true, out ParkItemCategory parsed) ? parsed : null;
    }

    public static ParkItemType? ToParkItemTypeFilter(this string? value)
    {
        return Enum.TryParse(value, true, out ParkItemType parsed) ? parsed : null;
    }

    public static RatingSummaryDto ToHttp(this RatingSummaryResult value)
    {
        return new RatingSummaryDto
        {
            TargetType = value.TargetType.ToString(),
            TargetId = value.TargetId,
            RatingCount = value.RatingCount,
            AverageRating = value.AverageRating,
            BayesianScore = value.BayesianScore,
            Rank = value.Rank,
        };
    }

    public static UserRatingDto ToHttp(this UserRatingResult value)
    {
        return new UserRatingDto
        {
            Id = value.Id,
            TargetType = value.TargetType.ToString(),
            TargetId = value.TargetId,
            ParkId = value.ParkId,
            ParkItemCategory = value.ParkItemCategory?.ToString(),
            ParkItemType = value.ParkItemType?.ToString(),
            Value = value.Value,
            CreatedAtUtc = value.CreatedAtUtc,
            UpdatedAtUtc = value.UpdatedAtUtc,
            Summary = value.Summary.ToHttp(),
        };
    }

    public static UserRatingListItemDto ToHttp(this UserRatingListItemResult value)
    {
        return new UserRatingListItemDto
        {
            Id = value.Id,
            TargetType = value.TargetType.ToString(),
            TargetId = value.TargetId,
            TargetName = value.TargetName,
            ParkId = value.ParkId,
            ParkName = value.ParkName,
            ParkItemCategory = value.ParkItemCategory?.ToString(),
            ParkItemType = value.ParkItemType?.ToString(),
            Value = value.Value,
            UpdatedAtUtc = value.UpdatedAtUtc,
            Summary = value.Summary.ToHttp(),
        };
    }

    public static UserRatingStatsDto ToHttp(this UserRatingStatsResult value)
    {
        return new UserRatingStatsDto
        {
            TotalRatings = value.TotalRatings,
            AverageRating = value.AverageRating,
            HighestRating = value.HighestRating,
            LowestRating = value.LowestRating,
            ByPark = value.ByPark.Select(static bucket => bucket.ToHttp()).ToList(),
            ByTargetType = value.ByTargetType.Select(static bucket => bucket.ToHttp()).ToList(),
            ByParkItemCategory = value.ByParkItemCategory.Select(static bucket => bucket.ToHttp()).ToList(),
        };
    }

    public static ParkRatingRankingDto ToHttp(this ParkRatingRankingResult value)
    {
        return new ParkRatingRankingDto
        {
            Rank = value.Rank,
            ParkId = value.ParkId,
            ParkName = value.ParkName,
            RatingCount = value.RatingCount,
            Score = value.Score,
            ParkRatingCount = value.ParkRatingCount,
            ParkAverageRating = value.ParkAverageRating,
            ItemsRatingCount = value.ItemsRatingCount,
            ItemsAverageRating = value.ItemsAverageRating,
            Categories = value.Categories.Select(static category => category.ToHttp()).ToList(),
        };
    }

    public static ParkItemRatingRankingDto ToHttp(this ParkItemRatingRankingResult value)
    {
        return new ParkItemRatingRankingDto
        {
            Rank = value.Rank,
            TargetId = value.TargetId,
            TargetName = value.TargetName,
            ParkId = value.ParkId,
            ParkName = value.ParkName,
            ParkItemCategory = value.ParkItemCategory.ToString(),
            ParkItemType = value.ParkItemType?.ToString(),
            RatingCount = value.RatingCount,
            AverageRating = value.AverageRating,
            BayesianScore = value.BayesianScore,
        };
    }

    public static UserParkRatingRankingDto ToHttp(this UserParkRatingRankingResult value)
    {
        return new UserParkRatingRankingDto
        {
            Rank = value.Rank,
            ParkId = value.ParkId,
            ParkName = value.ParkName,
            RatingCount = value.RatingCount,
            AverageRating = value.AverageRating,
            ParkRating = value.ParkRating?.ToHttp(),
            Categories = value.Categories.Select(static category => category.ToHttp()).ToList(),
        };
    }

    public static UserParkItemRatingRankingDto ToHttp(this UserParkItemRatingRankingResult value)
    {
        return new UserParkItemRatingRankingDto
        {
            Rank = value.Rank,
            Rating = value.Rating.ToHttp(),
        };
    }

    private static UserRatingStatBucketDto ToHttp(this UserRatingStatBucketResult value)
    {
        return new UserRatingStatBucketDto
        {
            Key = value.Key,
            Label = value.Label,
            Count = value.Count,
            AverageRating = value.AverageRating,
        };
    }

    private static ParkRatingRankingCategoryDto ToHttp(this ParkRatingRankingCategoryResult value)
    {
        return new ParkRatingRankingCategoryDto
        {
            ParkItemCategory = value.ParkItemCategory.ToString(),
            RatingCount = value.RatingCount,
            AverageRating = value.AverageRating,
            BayesianScore = value.BayesianScore,
            Items = value.Items.Select(static item => item.ToHttp()).ToList(),
        };
    }

    private static ParkRatingRankingItemDto ToHttp(this ParkRatingRankingItemResult value)
    {
        return new ParkRatingRankingItemDto
        {
            TargetId = value.TargetId,
            TargetName = value.TargetName,
            ParkItemCategory = value.ParkItemCategory?.ToString(),
            ParkItemType = value.ParkItemType?.ToString(),
            RatingCount = value.RatingCount,
            AverageRating = value.AverageRating,
            BayesianScore = value.BayesianScore,
        };
    }

    private static UserParkRatingRankingCategoryDto ToHttp(this UserParkRatingRankingCategoryResult value)
    {
        return new UserParkRatingRankingCategoryDto
        {
            ParkItemCategory = value.ParkItemCategory.ToString(),
            AverageRating = value.AverageRating,
            Items = value.Items.Select(static item => item.ToHttp()).ToList(),
        };
    }
}
