namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class PutTripDayPlanRequestDto
{
    public long ExpectedPlanVersion { get; set; }
    public long? ExpectedDayVersion { get; set; }
    public string ParkCandidateId { get; set; } = string.Empty;
    public string? DesiredArrivalTime { get; set; }
    public string? GroupNote { get; set; }
    public IReadOnlyCollection<TripDayBlockRequestDto> Blocks { get; set; } = Array.Empty<TripDayBlockRequestDto>();
}
