namespace AmusementPark.Application.Features.Ratings.Services;

public sealed record UserRankingShareOwner(
    string UserId,
    string DisplayName,
    DateTime PublishedAtUtc);
