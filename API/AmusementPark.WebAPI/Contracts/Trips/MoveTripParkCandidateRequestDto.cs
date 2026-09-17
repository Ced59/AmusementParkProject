namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class MoveTripParkCandidateRequestDto
{
    public long ExpectedPlanVersion { get; set; }
    public string? AnchorCandidateId { get; set; }
    public string Placement { get; set; } = string.Empty;
}
