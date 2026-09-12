namespace AmusementPark.Core.Domain.Visits;

public sealed record PassportRatingStatistics(
    long RatingCount,
    long HalfStepSum,
    double Average,
    double Median,
    double Minimum,
    double Maximum,
    double PopulationStandardDeviation);
