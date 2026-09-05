namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record UserRankingShareSettingsResult(
    bool IsPublic,
    string? ShareId,
    DateTime? PublishedAtUtc);
