namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record UserParkItemRatingRankingResult(
    int Rank,
    UserRatingListItemResult Rating);
