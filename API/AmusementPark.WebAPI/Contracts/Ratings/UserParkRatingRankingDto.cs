using System;
using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Ratings;

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
