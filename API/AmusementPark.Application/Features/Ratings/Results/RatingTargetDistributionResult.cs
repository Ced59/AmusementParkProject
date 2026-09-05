namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingTargetDistributionResult(
    string TargetType,
    string EvidenceBand,
    long TargetCount,
    long RatingObservationCount,
    long UniqueContributorCount);
