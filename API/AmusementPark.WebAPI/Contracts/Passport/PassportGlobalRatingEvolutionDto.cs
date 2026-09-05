namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportGlobalRatingEvolutionDto
{
    public int Year { get; init; }
    public double? ParkAverage { get; init; }
    public long RatedVisitCount { get; init; }
    public double? RideAverage { get; init; }
    public long RatedRideCount { get; init; }
}
