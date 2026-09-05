namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record SharedUserRankingProfileResult(
    string OwnerUserId,
    string DisplayName,
    DateTime PublishedAtUtc,
    UserRatingStatsResult Stats);
