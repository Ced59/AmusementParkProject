namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingRankingCategoryCoverageDto
{
    public string ScopeKey { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public int CandidateCount { get; set; }

    public int EligibleCount { get; set; }

    public bool HasMinimumComparableEntries { get; set; }
}
