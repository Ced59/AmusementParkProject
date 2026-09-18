namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripItemPreferenceDto
{
    public string ParkId { get; set; } = string.Empty;

    public string ParkName { get; set; } = string.Empty;

    public string ParkItemId { get; set; } = string.Empty;

    public string ParkItemName { get; set; } = string.Empty;

    public string? MainImageId { get; set; }

    public string Level { get; set; } = string.Empty;

    public string? Reason { get; set; }

    public long? Version { get; set; }
}
