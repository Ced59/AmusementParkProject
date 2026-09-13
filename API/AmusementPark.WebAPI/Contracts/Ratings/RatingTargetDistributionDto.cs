namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingTargetDistributionDto
{
    public string TargetType { get; set; } = string.Empty;

    public string EvidenceBand { get; set; } = string.Empty;

    public long TargetCount { get; set; }

    public long RatingObservationCount { get; set; }

    public long UniqueContributorCount { get; set; }
}
