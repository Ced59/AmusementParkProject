namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingRankingPublicationRulesDto
{
    public int MinimumEligibleEntries { get; set; }

    public decimal ScoreTieEpsilon { get; set; }

    public string RankingConvention { get; set; } = string.Empty;
}
