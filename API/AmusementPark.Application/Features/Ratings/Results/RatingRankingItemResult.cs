using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingRankingItemResult(
    RatingTargetType TargetType,
    string TargetId,
    string TargetName,
    string ParkId,
    string? ParkName,
    ParkItemCategory? ParkItemCategory,
    ParkItemType? ParkItemType,
    long RatingCount,
    double RatingSum,
    double AverageRating,
    double BayesianScore)
{
    public long? UniqueContributorCount { get; init; }

    public bool? AggregateIntegrityIsValid { get; init; }
}
