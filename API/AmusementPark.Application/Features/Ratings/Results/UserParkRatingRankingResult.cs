namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record UserParkRatingRankingResult(
    int Rank,
    string ParkId,
    string ParkName,
    int RatingCount,
    double AverageRating,
    UserRatingListItemResult? ParkRating,
    IReadOnlyCollection<UserParkRatingRankingCategoryResult> Categories);
