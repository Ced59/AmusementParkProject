namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingRankingPolicyTargetChangeDto
{
    public string TargetType { get; set; } = string.Empty;

    public string TargetId { get; set; } = string.Empty;

    public string TargetName { get; set; } = string.Empty;

    public int? PreviousRank { get; set; }

    public int? CandidateRank { get; set; }
}
