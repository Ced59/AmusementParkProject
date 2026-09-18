namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class BulkTripItemPreferenceRequestDto
{
    public string ParkItemId { get; set; } = string.Empty;

    public long? ExpectedPreferenceVersion { get; set; }

    public string Level { get; set; } = string.Empty;

    public string? Reason { get; set; }
}
