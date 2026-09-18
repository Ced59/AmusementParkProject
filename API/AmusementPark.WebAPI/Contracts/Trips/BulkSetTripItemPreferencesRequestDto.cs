namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class BulkSetTripItemPreferencesRequestDto
{
    public long ExpectedPlanVersion { get; set; }

    public List<BulkTripItemPreferenceRequestDto> Preferences { get; set; } = new();
}
