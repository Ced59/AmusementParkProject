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

    public long RatingCount { get; set; }

    public double AverageRating { get; set; }

    public double BayesianScore { get; set; }
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

public sealed class RatingRankingItemDto
{
    public int Rank { get; set; }

    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string TargetName { get; set; } = string.Empty;

    public string ParkId { get; set; } = string.Empty;

    public string? ParkName { get; set; }

    public string? ParkItemCategory { get; set; }

    public string? ParkItemType { get; set; }

    public long RatingCount { get; set; }

    public double AverageRating { get; set; }

    public double BayesianScore { get; set; }
}
