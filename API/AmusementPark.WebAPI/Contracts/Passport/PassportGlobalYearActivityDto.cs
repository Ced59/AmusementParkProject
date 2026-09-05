namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class PassportGlobalYearActivityDto
{
    public int Year { get; init; }
    public long VisitCount { get; init; }
    public long RecordedRideCount { get; init; }
}
