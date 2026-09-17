namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class AddTripParkCandidateRequestDto
{
    public long ExpectedPlanVersion { get; set; }
    public string ParkId { get; set; } = string.Empty;
    public IReadOnlyCollection<string> CandidateDates { get; set; } = Array.Empty<string>();
    public string Source { get; set; } = string.Empty;
    public string? CollectiveNote { get; set; }
}
