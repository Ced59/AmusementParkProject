namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportGlobalRatingEvolutionResult(
    int Year,
    double? ParkAverage,
    long RatedVisitCount,
    double? RideAverage,
    long RatedRideCount);
