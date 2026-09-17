namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripProgramDto
{
    public IReadOnlyCollection<TripParkCandidateDto> Candidates { get; set; } = Array.Empty<TripParkCandidateDto>();

    public IReadOnlyCollection<TripDayPlanDto> Days { get; set; } = Array.Empty<TripDayPlanDto>();
}
