namespace AmusementPark.Application.Features.Passport.Results;

public sealed record PassportRatingDistributionResult(
    long RatingCount,
    double Average,
    double Median,
    double Minimum,
    double Maximum,
    double PopulationStandardDeviation);
