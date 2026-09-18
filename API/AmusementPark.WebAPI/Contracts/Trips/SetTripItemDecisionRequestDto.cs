namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class SetTripItemDecisionRequestDto
{
    public long ExpectedPlanVersion { get; set; }

    public long? ExpectedDecisionVersion { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;
}
