namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportRatingDistributionDto
{
    public long RatingCount { get; init; }
    public double Average { get; init; }
    public double Median { get; init; }
    public double Minimum { get; init; }
    public double Maximum { get; init; }
    public double PopulationStandardDeviation { get; init; }
}
