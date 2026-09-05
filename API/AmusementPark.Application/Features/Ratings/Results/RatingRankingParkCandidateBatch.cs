namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record RatingRankingParkCandidateBatch(
    IReadOnlyCollection<string> ParkIds,
    bool IsTruncated);
