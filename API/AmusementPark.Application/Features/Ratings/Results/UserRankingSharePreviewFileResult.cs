namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record UserRankingSharePreviewFileResult(
    byte[] Content,
    string ContentType);
