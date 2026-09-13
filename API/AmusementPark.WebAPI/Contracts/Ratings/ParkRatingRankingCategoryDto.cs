using System;
using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class ParkRatingRankingCategoryDto
{
    public string ParkItemCategory { get; set; } = string.Empty;

    public long RatingCount { get; set; }

    public double AverageRating { get; set; }

    public double BayesianScore { get; set; }

    public IReadOnlyCollection<ParkRatingRankingItemDto> Items { get; set; } = Array.Empty<ParkRatingRankingItemDto>();
}
