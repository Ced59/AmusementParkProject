namespace AmusementPark.Application.Features.Ratings.Results;

public sealed record UserRatingStatBucketResult(
    string Key,
    string Label,
    long Count,
    double AverageRating);
