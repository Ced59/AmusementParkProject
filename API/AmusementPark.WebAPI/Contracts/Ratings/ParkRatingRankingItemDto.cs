using System;
using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Ratings;

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
