namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportCurrentItemRatingResult(
    string ParkItemId,
    double Rating,
    string? ParkItemName = null);
