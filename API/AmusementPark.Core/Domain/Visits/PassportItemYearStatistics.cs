namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportItemYearStatistics(
    int Year,
    long RideCount,
    long VisitCount,
    long RatedRideCount,
    double RatingCoverageRate,
    PassportRatingStatistics? Ratings);
