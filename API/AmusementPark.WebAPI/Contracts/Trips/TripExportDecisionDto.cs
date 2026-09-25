namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripExportDecisionDto
{
    public string? ParkName { get; set; }

    public string? ParkItemName { get; set; }

    public bool IsParkItemAvailable { get; set; }

    public string Status { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public DateTime DecidedAtUtc { get; set; }
}
