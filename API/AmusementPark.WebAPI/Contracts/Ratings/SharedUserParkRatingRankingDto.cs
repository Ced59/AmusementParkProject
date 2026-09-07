namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class SharedUserParkRatingRankingDto
{
    public int Rank { get; set; }

    public string ParkId { get; set; } = string.Empty;

    public string ParkName { get; set; } = string.Empty;

    public int RatingCount { get; set; }

    public double AverageRating { get; set; }

    public SharedUserRatingListItemDto? ParkRating { get; set; }

    public IReadOnlyCollection<SharedUserParkRatingRankingCategoryDto> Categories { get; set; } =
        Array.Empty<SharedUserParkRatingRankingCategoryDto>();
}
