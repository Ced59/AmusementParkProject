using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record ParkRatingRankingItemResult(
    string TargetId,
    string TargetName,
    ParkItemCategory? ParkItemCategory,
    ParkItemType? ParkItemType,
    long RatingCount,
    double AverageRating,
    double BayesianScore);
