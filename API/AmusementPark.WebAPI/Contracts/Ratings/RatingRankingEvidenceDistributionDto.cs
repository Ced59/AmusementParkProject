namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingRankingEvidenceDistributionDto
{
    public string TargetType { get; set; } = string.Empty;

    public string Level { get; set; } = string.Empty;

    public int TargetCount { get; set; }

    public long UniqueContributorCount { get; set; }

    public long RatingObservationCount { get; set; }
}
