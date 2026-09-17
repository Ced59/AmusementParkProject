namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripPlanWriteRequestDto
{
    public string Title { get; set; } = string.Empty;

    public TripDateProposalRequestDto DateProposal { get; set; } = new();

    public string? DestinationTimeZoneId { get; set; }
}
