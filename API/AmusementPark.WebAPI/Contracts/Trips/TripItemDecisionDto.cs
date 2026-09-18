namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripItemDecisionDto
{
    public string Status { get; set; } = string.Empty;

    public string Reason { get; set; } = string.Empty;

    public string DecidedByDisplayName { get; set; } = string.Empty;

    public DateTime DecidedAtUtc { get; set; }

    public long Version { get; set; }
}
