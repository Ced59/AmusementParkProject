using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record ParkRatingRankingCategoryResult(
    ParkItemCategory ParkItemCategory,
    long RatingCount,
    double AverageRating,
    double BayesianScore,
    IReadOnlyCollection<ParkRatingRankingItemResult> Items);
