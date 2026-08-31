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

    public static RatingDiagnosticsDto ToHttp(this RatingDiagnosticsResult value)
    {
        return new RatingDiagnosticsDto
        {
            GeneratedAtUtc = value.GeneratedAtUtc,
            ExecutionDurationMilliseconds = value.ExecutionDurationMilliseconds,
            TotalRatings = value.TotalRatings,
            DistinctNumericValueCount = value.DistinctNumericValueCount,
            DistinctNumericValueSample = value.DistinctNumericValueSample,
            IsDistinctNumericValueSampleTruncated = value.IsDistinctNumericValueSampleTruncated,
            Anomalies = new RatingAnomalySummaryDto
            {
                NonNumericValueCount = value.Anomalies.NonNumericValueCount,
                UnexpectedValueStorageTypeCount = value.Anomalies.UnexpectedValueStorageTypeCount,
                OutOfRangeValueCount = value.Anomalies.OutOfRangeValueCount,
                NonHalfStepValueCount = value.Anomalies.NonHalfStepValueCount,
                NearHalfStepValueCount = value.Anomalies.NearHalfStepValueCount,
                MissingUserIdCount = value.Anomalies.MissingUserIdCount,
                MissingTargetCount = value.Anomalies.MissingTargetCount,
                DuplicateVoteKeyCount = value.Anomalies.DuplicateVoteKeyCount,
                ExtraDuplicateDocumentCount = value.Anomalies.ExtraDuplicateDocumentCount,
            },
            AggregateIntegrity = new RatingAggregateIntegrityDto
            {
                IsSourceComparisonEvaluated = value.AggregateIntegrity.IsSourceComparisonEvaluated,
                IsOrphanCheckEvaluated = value.AggregateIntegrity.IsOrphanCheckEvaluated,
                SourceTargetCount = value.AggregateIntegrity.SourceTargetCount,
                MissingAggregateCount = value.AggregateIntegrity.MissingAggregateCount,
                DivergentAggregateCount = value.AggregateIntegrity.DivergentAggregateCount,
                ContributorCountMismatchCount = value.AggregateIntegrity.ContributorCountMismatchCount,
                OrphanAggregateCount = value.AggregateIntegrity.OrphanAggregateCount,
            },
            TargetDistribution = value.TargetDistribution.Select(static item => new RatingTargetDistributionDto
            {
                TargetType = item.TargetType,
                EvidenceBand = item.EvidenceBand,
                TargetCount = item.TargetCount,
                RatingObservationCount = item.RatingObservationCount,
                UniqueContributorCount = item.UniqueContributorCount,
            }).ToList(),
            Indexes = value.Indexes.Select(static index => new RatingIndexStatusDto
            {
                Collection = index.Collection,
                Name = index.Name,
                IsPresent = index.IsPresent,
                IsUnique = index.IsUnique,
                IsHidden = index.IsHidden,
                HasUnexpectedOptions = index.HasUnexpectedOptions,
                MatchesExpectedDefinition = index.MatchesExpectedDefinition,
                ExpectedKeys = index.ExpectedKeys,
                ActualKeys = index.ActualKeys,
            }).ToList(),
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

    public static UserRankingShareSettingsDto ToHttp(this UserRankingShareSettingsResult value)
    {
        return new UserRankingShareSettingsDto
        {
            IsPublic = value.IsPublic,
            ShareId = value.ShareId,
            PublishedAtUtc = value.PublishedAtUtc,
        };
    }

    public static SharedUserRankingProfileDto ToHttp(
        this SharedUserRankingProfileResult value,
        string? currentUserId)
    {
        return new SharedUserRankingProfileDto
        {
            DisplayName = value.DisplayName,
            PublishedAtUtc = value.PublishedAtUtc,
            IsOwner = !string.IsNullOrWhiteSpace(currentUserId)
                && string.Equals(value.OwnerUserId, currentUserId, StringComparison.Ordinal),
            Stats = value.Stats.ToHttp(),
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
