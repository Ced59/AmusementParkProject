namespace AmusementPark.WebAPI.Contracts.Ratings;

public sealed class RatingAggregateIntegrityDto
{
    public bool IsSourceComparisonEvaluated { get; set; }

    public bool IsOrphanCheckEvaluated { get; set; }

    public long SourceTargetCount { get; set; }

    public long MissingAggregateCount { get; set; }

    public long DivergentAggregateCount { get; set; }

    public long ContributorCountMismatchCount { get; set; }

    public long DerivedScoreMismatchCount { get; set; }

    public long OrphanAggregateCount { get; set; }
}
