namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportGlobalRatingEvolution(
    int Year,
    double? ParkAverage,
    long RatedVisitCount,
    double? RideAverage,
    long RatedRideCount);
