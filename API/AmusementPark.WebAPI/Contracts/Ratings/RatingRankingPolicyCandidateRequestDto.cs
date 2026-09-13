namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingRankingPolicyCandidateRequestDto
{
    public string Version { get; set; } = string.Empty;

    public int ProvisionalMinUniqueContributors { get; set; }

    public int EligibleMinUniqueContributors { get; set; }

    public int EstablishedMinUniqueContributors { get; set; }

    public int StrongEvidenceMinUniqueContributors { get; set; }

    public int MinimumEligibleEntriesPerRanking { get; set; }

    public int MinimumEligibleItemsForParkItemComponent { get; set; }

    public int MinimumEligibleItemsPerCategory { get; set; }

    public int MinimumEligibleCategories { get; set; }

    public decimal ScoreTieEpsilon { get; set; }
}
