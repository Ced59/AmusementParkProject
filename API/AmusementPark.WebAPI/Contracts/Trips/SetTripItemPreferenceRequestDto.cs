namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class SetTripItemPreferenceRequestDto
{
    public long ExpectedPlanVersion { get; set; }

    public long? ExpectedPreferenceVersion { get; set; }

    public string Level { get; set; } = string.Empty;

    public string? Reason { get; set; }
}
