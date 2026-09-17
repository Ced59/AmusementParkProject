namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class SetTripPlanDatesRequestDto
{
    public long ExpectedVersion { get; set; }

    public TripDateProposalRequestDto DateProposal { get; set; } = new();

    public string? DestinationTimeZoneId { get; set; }
}
