namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportGlobalItemActivityResult(
    string ParkItemId,
    string? ParkItemName,
    string ParkId,
    string? ParkName,
    long CompletedRideCount);
