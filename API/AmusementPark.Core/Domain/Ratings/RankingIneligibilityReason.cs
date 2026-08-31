namespace AmusementPark.Core.Domain.Ratings;

/// <summary>
/// Raison métier stable expliquant l'absence d'un rang communautaire.
/// </summary>
public enum RankingIneligibilityReason
{
    NoRatings = 0,
    TooFewUniqueContributors = 1,
    TooFewComparableEntries = 2,
    InsufficientItemCoverage = 3,
    InsufficientCategoryCoverage = 4,
    TargetUnavailable = 5,
    TargetExcluded = 6,
    AggregateIntegrityFailure = 7,
    UnsupportedComposition = 8,
}
