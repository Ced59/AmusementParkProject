using System;
using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class UserRatingUpsertDto
{
    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public double Value { get; set; }
}

public sealed class RatingSummaryDto
{
    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    /// <summary>Alias historique du nombre d'observations retenues pour cette cible simple.</summary>
    public long RatingCount { get; set; }

    public long RatingObservationCount { get; set; }

    public long? UniqueContributorCount { get; set; }

    public double AverageRating { get; set; }

    public double BayesianScore { get; set; }

    public int? Rank { get; set; }

    public RankingEvidenceDto? Evidence { get; set; }

    public string? MethodologyVersion { get; set; }
}

public sealed class RankingEvidenceDto
{
    public string Level { get; set; } = string.Empty;

    public bool IsEligibleForMainRanking { get; set; }

    public long? DirectParkContributorCount { get; set; }

    public long? ItemContributorCount { get; set; }

    public int? EligibleItemCount { get; set; }

    public int? EligibleCategoryCount { get; set; }

    public string? IneligibilityReason { get; set; }

    public int? NextThreshold { get; set; }
}

public sealed class UserRatingDto
{
    public string Id { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string ParkId { get; set; } = string.Empty;

    public string? ParkItemCategory { get; set; }

    public string? ParkItemType { get; set; }

    public double Value { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public RatingSummaryDto Summary { get; set; } = new RatingSummaryDto();
}

public sealed class UserRatingListItemDto
{
    public string Id { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string TargetName { get; set; } = string.Empty;

    public string ParkId { get; set; } = string.Empty;

    public string? ParkName { get; set; }

    public string? ParkItemCategory { get; set; }

    public string? ParkItemType { get; set; }

    public double Value { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public RatingSummaryDto Summary { get; set; } = new RatingSummaryDto();
}

public sealed class UserRatingStatBucketDto
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public long Count { get; set; }

    public double AverageRating { get; set; }
}

public sealed class UserRatingStatsDto
{
    public long TotalRatings { get; set; }

    public double AverageRating { get; set; }

    public double HighestRating { get; set; }

    public double LowestRating { get; set; }

    public IReadOnlyCollection<UserRatingStatBucketDto> ByPark { get; set; } = Array.Empty<UserRatingStatBucketDto>();

    public IReadOnlyCollection<UserRatingStatBucketDto> ByTargetType { get; set; } = Array.Empty<UserRatingStatBucketDto>();

    public IReadOnlyCollection<UserRatingStatBucketDto> ByParkItemCategory { get; set; } = Array.Empty<UserRatingStatBucketDto>();
}

public sealed class ParkRatingRankingItemDto
{
    public string TargetId { get; set; } = string.Empty;

    public string TargetName { get; set; } = string.Empty;

    public string? ParkItemCategory { get; set; }

    public string? ParkItemType { get; set; }

    public long RatingCount { get; set; }

    public double AverageRating { get; set; }

    public double BayesianScore { get; set; }
}

public sealed class ParkRatingRankingCategoryDto
{
    public string ParkItemCategory { get; set; } = string.Empty;

    public long RatingCount { get; set; }

    public double AverageRating { get; set; }

    public double BayesianScore { get; set; }

    public IReadOnlyCollection<ParkRatingRankingItemDto> Items { get; set; } = Array.Empty<ParkRatingRankingItemDto>();
}

public sealed class ParkRatingRankingDto
{
    public int Rank { get; set; }

    public string ParkId { get; set; } = string.Empty;

    public string ParkName { get; set; } = string.Empty;

    /// <summary>Alias historique du nombre d'observations retenues dans le score composé du parc.</summary>
    public long RatingCount { get; set; }

    public long RatingObservationCount { get; set; }

    public long? UniqueContributorCount { get; set; }

    public double Score { get; set; }

    public long ParkRatingCount { get; set; }

    public double ParkAverageRating { get; set; }

    public long ItemsRatingCount { get; set; }

    public double ItemsAverageRating { get; set; }

    public RankingEvidenceDto? Evidence { get; set; }

    public string? MethodologyVersion { get; set; }

    public IReadOnlyCollection<ParkRatingRankingCategoryDto> Categories { get; set; } = Array.Empty<ParkRatingRankingCategoryDto>();
}

public sealed class ParkItemRatingRankingDto
{
    public int Rank { get; set; }

    public string TargetId { get; set; } = string.Empty;

    public string TargetName { get; set; } = string.Empty;

    public string ParkId { get; set; } = string.Empty;

    public string ParkName { get; set; } = string.Empty;

    public string ParkItemCategory { get; set; } = string.Empty;

    public string? ParkItemType { get; set; }

    /// <summary>Alias historique du nombre d'observations retenues pour cet élément.</summary>
    public long RatingCount { get; set; }

    public long RatingObservationCount { get; set; }

    public long? UniqueContributorCount { get; set; }

    public double AverageRating { get; set; }

    public double BayesianScore { get; set; }

    public RankingEvidenceDto? Evidence { get; set; }

    public string? MethodologyVersion { get; set; }
}

public sealed class UserParkRatingRankingCategoryDto
{
    public string ParkItemCategory { get; set; } = string.Empty;

    public double AverageRating { get; set; }

    public IReadOnlyCollection<UserRatingListItemDto> Items { get; set; } = Array.Empty<UserRatingListItemDto>();
}

public sealed class UserParkRatingRankingDto
{
    public int Rank { get; set; }

    public string ParkId { get; set; } = string.Empty;

    public string ParkName { get; set; } = string.Empty;

    public int RatingCount { get; set; }

    public double AverageRating { get; set; }

    public UserRatingListItemDto? ParkRating { get; set; }

    public IReadOnlyCollection<UserParkRatingRankingCategoryDto> Categories { get; set; } = Array.Empty<UserParkRatingRankingCategoryDto>();
}

public sealed class UserParkItemRatingRankingDto
{
    public int Rank { get; set; }

    public UserRatingListItemDto Rating { get; set; } = new UserRatingListItemDto();
}

public sealed class UserRankingShareVisibilityDto
{
    public bool IsPublic { get; set; }
}

public sealed class UserRankingShareSettingsDto
{
    public bool IsPublic { get; set; }

    public string? ShareId { get; set; }

    public DateTime? PublishedAtUtc { get; set; }
}

public sealed class SharedUserRankingProfileDto
{
    public string DisplayName { get; set; } = string.Empty;

    public DateTime PublishedAtUtc { get; set; }

    public bool IsOwner { get; set; }

    public UserRatingStatsDto Stats { get; set; } = new UserRatingStatsDto();
}
