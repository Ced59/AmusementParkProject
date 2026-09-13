namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportRatingCoverageDto
{
    public long RatedCount { get; init; }
    public long TotalCount { get; init; }
    public double Rate { get; init; }
}
