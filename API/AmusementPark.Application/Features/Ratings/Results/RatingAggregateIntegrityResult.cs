namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingAggregateIntegrityResult(
    bool IsSourceComparisonEvaluated,
    bool IsOrphanCheckEvaluated,
    long SourceTargetCount,
    long MissingAggregateCount,
    long DivergentAggregateCount,
    long ContributorCountMismatchCount,
    long DerivedScoreMismatchCount,
    long OrphanAggregateCount);
