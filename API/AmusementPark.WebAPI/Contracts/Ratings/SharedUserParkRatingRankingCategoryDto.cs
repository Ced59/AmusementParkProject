namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class SharedUserParkRatingRankingCategoryDto
{
    public string ParkItemCategory { get; set; } = string.Empty;

    public double AverageRating { get; set; }

    public IReadOnlyCollection<SharedUserRatingListItemDto> Items { get; set; } =
        Array.Empty<SharedUserRatingListItemDto>();
}
