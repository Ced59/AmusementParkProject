using System;
using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class UserParkRatingRankingCategoryDto
{
    public string ParkItemCategory { get; set; } = string.Empty;

    public double AverageRating { get; set; }

    public IReadOnlyCollection<UserRatingListItemDto> Items { get; set; } = Array.Empty<UserRatingListItemDto>();
}
