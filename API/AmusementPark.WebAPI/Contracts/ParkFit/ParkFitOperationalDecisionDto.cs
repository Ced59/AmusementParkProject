namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class ParkFitOperationalDecisionDto
{
    public string Type { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public DateTime DecidedAtUtc { get; init; }
    public long Revision { get; init; }
}
