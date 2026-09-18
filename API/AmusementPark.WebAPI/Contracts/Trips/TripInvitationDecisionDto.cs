namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripInvitationDecisionDto
{
    public string TripPlanId { get; set; } = string.Empty;

    public bool WasReplayed { get; set; }
}
