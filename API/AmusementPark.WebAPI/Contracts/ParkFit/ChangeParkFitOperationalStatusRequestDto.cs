namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class ChangeParkFitOperationalStatusRequestDto
{
    public string TargetState { get; init; } = string.Empty;

    public string Reason { get; init; } = string.Empty;

    public long ExpectedRevision { get; init; }
}
