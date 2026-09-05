using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record UserParkRatingRankingCategoryResult(
    ParkItemCategory ParkItemCategory,
    double AverageRating,
    IReadOnlyCollection<UserRatingListItemResult> Items);
