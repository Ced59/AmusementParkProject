namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripDateProposalRequestDto
{
    public string Kind { get; set; } = string.Empty;

    public string? StartDate { get; set; }

    public string? EndDate { get; set; }

    public IReadOnlyCollection<string> CandidateDates { get; set; } = Array.Empty<string>();
}
