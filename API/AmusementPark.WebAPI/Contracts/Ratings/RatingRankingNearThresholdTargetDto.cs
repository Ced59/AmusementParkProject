namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingRankingNearThresholdTargetDto
{
    public string ScopeKey { get; set; } = string.Empty;

    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string TargetName { get; set; } = string.Empty;

    public int UniqueContributorCount { get; set; }

    public int EligibilityThreshold { get; set; }

    public int RemainingContributorCount { get; set; }
}
