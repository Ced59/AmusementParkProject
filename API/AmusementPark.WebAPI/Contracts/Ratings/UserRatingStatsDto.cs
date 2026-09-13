using System;
using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Ratings;

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
