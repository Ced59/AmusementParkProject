namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class SharedUserRatingStatsDto
{
    public long TotalRatings { get; set; }

    public double AverageRating { get; set; }

    public double HighestRating { get; set; }

    public double LowestRating { get; set; }
}
