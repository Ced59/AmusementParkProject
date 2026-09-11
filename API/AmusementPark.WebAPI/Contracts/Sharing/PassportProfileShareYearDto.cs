namespace AmusementPark.WebAPI.Contracts.Sharing;

public sealed class PassportProfileShareYearDto
{
    public int Year { get; set; }

    public long VisitCount { get; set; }

    public long ParkCount { get; set; }

    public long? CompletedRideCount { get; set; }
}
