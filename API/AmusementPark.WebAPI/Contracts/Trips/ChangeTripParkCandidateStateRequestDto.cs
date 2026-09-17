namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class ChangeTripParkCandidateStateRequestDto
{
    public long ExpectedPlanVersion { get; set; }
    public long ExpectedCandidateVersion { get; set; }
    public string State { get; set; } = string.Empty;
}
