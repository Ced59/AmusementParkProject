namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingRankingExclusionDistributionDto
{
    public string TargetType { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public int TargetCount { get; set; }
}
