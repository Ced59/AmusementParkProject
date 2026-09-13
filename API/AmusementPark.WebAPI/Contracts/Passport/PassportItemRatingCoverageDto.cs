namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportItemRatingCoverageDto
{
    public long RatedRideCount { get; init; }

    public long TotalRideCount { get; init; }

    public double Rate { get; init; }
}
