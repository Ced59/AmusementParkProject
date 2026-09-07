namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class SharedUserParkItemRatingRankingDto
{
    public int Rank { get; set; }

    public SharedUserRatingListItemDto Rating { get; set; } = new SharedUserRatingListItemDto();
}
