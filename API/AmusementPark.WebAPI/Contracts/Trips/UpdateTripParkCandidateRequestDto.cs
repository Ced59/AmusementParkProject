namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class UpdateTripParkCandidateRequestDto
{
    public long ExpectedPlanVersion { get; set; }
    public long ExpectedCandidateVersion { get; set; }
    public IReadOnlyCollection<string> CandidateDates { get; set; } = Array.Empty<string>();
    public string? CollectiveNote { get; set; }
}
