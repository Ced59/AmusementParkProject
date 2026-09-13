using System;
using System.Collections.Generic;

namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class UserParkItemRatingRankingDto
{
    public int Rank { get; set; }

    public UserRatingListItemDto Rating { get; set; } = new UserRatingListItemDto();
}
