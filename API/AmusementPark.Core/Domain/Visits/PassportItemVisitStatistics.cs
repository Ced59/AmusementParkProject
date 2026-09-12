namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportItemVisitStatistics(
    string VisitId,
    VisitDate VisitDate,
    long RideCount,
    long RatedRideCount,
    double RatingCoverageRate,
    PassportRatingStatistics? Ratings);
