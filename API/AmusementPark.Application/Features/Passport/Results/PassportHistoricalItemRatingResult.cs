namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportHistoricalItemRatingResult(
    string ParkItemId,
    long RatingCount,
    double Average,
    string? ParkItemName = null);
