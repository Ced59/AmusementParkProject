namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class RenameTripPlanRequestDto
{
    public long ExpectedVersion { get; set; }

    public string Title { get; set; } = string.Empty;
}
